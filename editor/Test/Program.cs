// See https://aka.ms/new-console-template for more information
using Meanscript;

Console.WriteLine("Hello, World!");
const int n = 10;
IntArray a = new IntArray(n);
for (int i=0; i<n; i++)
{
	a[i]='a'+i;
	Console.WriteLine("> "+a[i]);
}

