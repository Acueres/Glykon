using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Reflection;
using Tython.Model;

namespace Tython
{
    public class CodeEmitter(Statement[] statements, string appname)
    {
        readonly Statement[] statements = statements;
        readonly string appname = appname;

        private static readonly Guid Guid = new("87D4DBE1-1143-4FAD-AAB3-1001F92068E6");
        private static readonly BlobContentId ContentId = new(Guid, 0x04030201);

        readonly MetadataBuilder metadata = new();
        readonly BlobBuilder ilBuilder = new();
        MethodDefinitionHandle entryPoint;

        public void EmitAssembly()
        {
            entryPoint = EmitEntryPoint();
        }

        MethodDefinitionHandle EmitEntryPoint()
        {
            // Create module and assembly for a console application.
            metadata.AddModule(
                0,
                metadata.GetOrAddString($"{appname}.exe"),
                metadata.GetOrAddGuid(Guid),
                default,
                default);

            metadata.AddAssembly(
                metadata.GetOrAddString(appname),
                version: new Version(1, 0, 0, 0),
                culture: default,
                publicKey: default,
                flags: 0,
                hashAlgorithm: AssemblyHashAlgorithm.None);

            // Create references to System.Object and System.Console types.
            AssemblyReferenceHandle mscorlibAssemblyRef = metadata.AddAssemblyReference(
                name: metadata.GetOrAddString("mscorlib"),
                version: new Version(4, 0, 0, 0),
                culture: default,
                publicKeyOrToken: metadata.GetOrAddBlob(
                    new byte[] { 0xB7, 0x7A, 0x5C, 0x56, 0x19, 0x34, 0xE0, 0x89 }
                    ),
                flags: default,
                hashValue: default);

            TypeReferenceHandle systemObjectTypeRef = metadata.AddTypeReference(
                mscorlibAssemblyRef,
                metadata.GetOrAddString("System"),
                metadata.GetOrAddString("Object"));

            TypeReferenceHandle systemConsoleTypeRefHandle = metadata.AddTypeReference(
                mscorlibAssemblyRef,
                metadata.GetOrAddString("System"),
                metadata.GetOrAddString("Console"));

            // Get reference to Console.WriteLine(string) method.
            var consoleWriteLineSignature = new BlobBuilder();

            new BlobEncoder(consoleWriteLineSignature).
                MethodSignature().
                Parameters(1,
                    returnType => returnType.Void(),
                    parameters => parameters.AddParameter().Type().String());

            MemberReferenceHandle consoleWriteLineMemberRef = metadata.AddMemberReference(
                systemConsoleTypeRefHandle,
                metadata.GetOrAddString("WriteLine"),
                metadata.GetOrAddBlob(consoleWriteLineSignature));

            // Get reference to Object's constructor.
            var parameterlessCtorSignature = new BlobBuilder();

            new BlobEncoder(parameterlessCtorSignature).
                MethodSignature(isInstanceMethod: true).
                Parameters(0, returnType => returnType.Void(), parameters => { });

            BlobHandle parameterlessCtorBlobIndex = metadata.GetOrAddBlob(parameterlessCtorSignature);

            MemberReferenceHandle objectCtorMemberRef = metadata.AddMemberReference(
                systemObjectTypeRef,
                metadata.GetOrAddString(".ctor"),
                parameterlessCtorBlobIndex);

            // Create signature for "void Main()" method.
            var mainSignature = new BlobBuilder();

            new BlobEncoder(mainSignature).
                MethodSignature().
                Parameters(0, returnType => returnType.Void(), parameters => { });

            var methodBodyStream = new MethodBodyStreamEncoder(ilBuilder);

            var codeBuilder = new BlobBuilder();
            InstructionEncoder il;

            // Emit IL for Program::.ctor
            il = new InstructionEncoder(codeBuilder);

            // ldarg.0
            il.LoadArgument(0);

            // call instance void [mscorlib]System.Object::.ctor()
            il.Call(objectCtorMemberRef);

            // ret
            il.OpCode(ILOpCode.Ret);

            int ctorBodyOffset = methodBodyStream.AddMethodBody(il);
            codeBuilder.Clear();

            // Emit IL for Program::Main
            var flowBuilder = new ControlFlowBuilder();
            il = new InstructionEncoder(codeBuilder, flowBuilder);

            foreach (Statement statement in statements)
            {
                if (statement.Token.Lexeme == "print")
                {
                    EmitPrintStatement(statement, il, consoleWriteLineMemberRef);
                }
            }

            // ret
            il.OpCode(ILOpCode.Ret);

            int mainBodyOffset = methodBodyStream.AddMethodBody(il);
            codeBuilder.Clear();

            // Create method definition for Program::Main
            MethodDefinitionHandle mainMethodDef = metadata.AddMethodDefinition(
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                MethodImplAttributes.IL,
                metadata.GetOrAddString("Main"),
                metadata.GetOrAddBlob(mainSignature),
                mainBodyOffset,
                parameterList: default);

            // Create method definition for Program::.ctor
            MethodDefinitionHandle ctorDef = metadata.AddMethodDefinition(
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                MethodImplAttributes.IL,
                metadata.GetOrAddString(".ctor"),
                parameterlessCtorBlobIndex,
                ctorBodyOffset,
                parameterList: default);

            // Create type definition for the special <Module> type that holds global functions
            metadata.AddTypeDefinition(
                default,
                default,
                metadata.GetOrAddString("<Module>"),
                baseType: default,
                fieldList: MetadataTokens.FieldDefinitionHandle(1),
                methodList: mainMethodDef);

            // Create type definition for Appname.Program
            metadata.AddTypeDefinition(
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.BeforeFieldInit,
                metadata.GetOrAddString(appname),
                metadata.GetOrAddString("Program"),
                baseType: systemObjectTypeRef,
                fieldList: MetadataTokens.FieldDefinitionHandle(1),
                methodList: mainMethodDef);
            var s = mainMethodDef.ToDebugInformationHandle();

            return mainMethodDef;
        }

        public void Save()
        {
            using Stream peStream = new FileStream($"{appname}.exe", FileMode.OpenOrCreate, FileAccess.ReadWrite);
            // Create executable with the managed metadata from the specified MetadataBuilder.
            var peHeaderBuilder = new PEHeaderBuilder(
                imageCharacteristics: Characteristics.ExecutableImage
                );

            var peBuilder = new ManagedPEBuilder(
                peHeaderBuilder,
                new MetadataRootBuilder(metadata),
                ilBuilder,
                entryPoint: entryPoint,
                flags: CorFlags.ILOnly,
                deterministicIdProvider: content => ContentId);

            // Write executable into the specified stream.
            var peBlob = new BlobBuilder();
            BlobContentId contentId = peBuilder.Serialize(peBlob);
            peBlob.WriteContentTo(peStream);
        }

        public MemoryStream ToStream()
        {
            MemoryStream peStream = new();
            // Create executable with the managed metadata from the specified MetadataBuilder.
            var peHeaderBuilder = new PEHeaderBuilder(
                imageCharacteristics: Characteristics.ExecutableImage
                );

            var peBuilder = new ManagedPEBuilder(
                peHeaderBuilder,
                new MetadataRootBuilder(metadata),
                ilBuilder,
                entryPoint: entryPoint,
                flags: CorFlags.ILOnly,
                deterministicIdProvider: content => ContentId);

            // Write executable into the specified stream.
            var peBlob = new BlobBuilder();
            BlobContentId contentId = peBuilder.Serialize(peBlob);
            peBlob.WriteContentTo(peStream);
            return peStream;
        }

        void EmitPrintStatement(Statement statement, InstructionEncoder il, MemberReferenceHandle consoleWriteLineMemberRef)
        {

            EmitExpression(statement.Expression, il);

            // call void [mscorlib]System.Console::WriteLine(string)
            il.Call(consoleWriteLineMemberRef);
        }

        void EmitExpression(Expression expression, InstructionEncoder il)
        {
            switch (expression.Type)
            {
                case ExpressionType.Literal:
                    {
                        switch (expression.Token.Type)
                        {
                            case TokenType.String:
                                il.LoadString(metadata.GetOrAddUserString(expression.Token.Lexeme));
                                break;
                        }
                        break;
                    }
            }
        }
    }
}
