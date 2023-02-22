// See https://aka.ms/new-console-template for more information
using CodeExMachina;

Console.WriteLine("B-tree test");

const int DATA_SIZE = 2000;
int [] suffle = new int[DATA_SIZE];
Random rnd = new Random();
for (int i=0; i<suffle.Length; i++) suffle[i] = i;
for (int i = suffle.Length - 1; i > 0; i--)
{
	int j = rnd.Next(i + 1);
	int temp = suffle[i];
	suffle[i] = suffle[j];
	suffle[j] = temp;
}


var watch = new System.Diagnostics.Stopwatch();
int sum = 0;

for (int foo = 0; foo < 1; foo++)
{
	//BTree<Integer> tr = new(8, new IntegerComparer());
	//sum = 0;
	//watch.Restart();
	//for (int i=0; i<DATA_SIZE; i++) _ = tr.ReplaceOrInsert(new Integer(i));
	//for (int i=0; i<DATA_SIZE; i++) sum += tr.Get(new Integer(arr[i])).Value;
	//Console.WriteLine("B-tree: " + watch.ElapsedMilliseconds + " sum: " + sum);
	
	Dictionary<int,int> dict = new();
	sum = 0;
	watch.Restart();
	for (int i=0; i<DATA_SIZE; i++) _ = dict[suffle[i]]=i;
	for (int i=0; i<DATA_SIZE; i++)
	{
		int key = suffle[i];
		sum += dict[key];
		//Console.WriteLine(dict[key].ToString());
	}
	Console.WriteLine("Dictionary: " + watch.ElapsedMilliseconds + " sum: " + sum);

	sum = 0;
	watch.Restart();
	int [] a = new int [DATA_SIZE];
	for (int i=0; i<DATA_SIZE; i++) _ = a[suffle[i]]=i;
	for (int i=0; i<DATA_SIZE; i++)
	{
		int key = suffle[i];
		for (int bar=0; bar<DATA_SIZE; bar++)
		{
			if (a[bar] == key)
			{
				sum += a[bar];
				//Console.WriteLine(a[bar].ToString());
				break;
			}
		}
	}
	Console.WriteLine("Array: " + watch.ElapsedMilliseconds + " sum: " + sum);
}