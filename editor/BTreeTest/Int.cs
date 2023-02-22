class Integer  
{
    private readonly int _v;

    public int Value => _v;

    public Integer(int v)
    {
        _v = v;
    }

    public override string ToString() => _v.ToString();             
}

// The Comparer must provide a strict weak ordering.
//
// If !(x < y) and !(y < x), we treat this to mean x == y 
// (i.e. we can only hold one of either x or y in the tree).

class IntegerComparer : Comparer<Integer>
{
    public override int Compare(Integer x, Integer y)
    {
        return x.Value < y.Value ? -1 : x.Value > y.Value ? 1 : 0;
    }
}
