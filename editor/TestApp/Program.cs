using Meanscript;
public class MeanscriptTestApp
{
    static void Main(string[] args)
    {
		var code = new MSCode("int a: 5");
		Console.WriteLine("a = " + code.global["a"].Int());
	}
}