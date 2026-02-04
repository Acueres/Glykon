using Grpc.Core;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

using Glykon.Compiler.Core;

using Glykon.Compiler.Syntax;

namespace Glykon.LanguageService.Services;

public class CompilerServiceImpl : CompilerService.CompilerServiceBase
{
    private static readonly ByteString specByteString = ByteString.CopyFrom(LanguageSpec.ToJsonUtf8());

    public override Task<LanguageSpecReply> GetLanguageSpec(Empty request, ServerCallContext context)
    {
        var reply = new LanguageSpecReply
        {
            Language = LanguageSpec.LanguageName,
            Version = LanguageSpec.SpecVersion,
            Json = specByteString,
        };
        return Task.FromResult(reply);
    }

    public override Task<PredictReply> PredictNext(PredictRequest request, ServerCallContext context)
    {
        var text = request.Text ?? string.Empty;
        
        const string filename = "<rpc>";
        SourceText sourceText = new(filename, text);
        var lexer = new Lexer(sourceText, sourceText.FileName, SyntaxMode.Predict);
        var tokens = lexer.Lex();
        
        var parser = new Parser(tokens, filename: sourceText.FileName, mode: SyntaxMode.Predict);
        var result = parser.Parse();
        
        var expected = result.Expected ?? [];
        
        bool canTerminate = expected.Contains(TokenKind.VirtualTerminator);
        bool canEndInput = expected.Contains(TokenKind.EOF);

        var reply = new PredictReply
        {
            CanTerminateStatement = canTerminate,
            CanEndInput = canEndInput,
        };

        foreach (var k in expected)
        {
            if (k == TokenKind.VirtualTerminator) continue;
            reply.ExpectedTokenKindIds.Add((int)k);
        }

        return Task.FromResult(reply);
    }
}