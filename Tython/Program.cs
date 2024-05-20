namespace Tython
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var codeEmitter = new CodeEmitter([], "HelloTython");
            codeEmitter.EmitAssembly();
            codeEmitter.Save();
        }
    }
}
