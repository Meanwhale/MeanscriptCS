// (C) 2018, single-linked list

// TODO
//		- Insert: before or after curret?
//		- Exceptions and exception tests.
//		- Prevent concurrent modification. List operations and iterators have unique IDs.
//		- Add/remove by index.
//		- Iterator/enumerator with a condition.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Meanscript
{
public class MConcurrentModificationException : Exception
{
}

public class MListNode<T>
{
	public T value;
	public MListNode<T> Next;
}

public class MListIterator<T>
{
	private int listState;
	private MList<T> list;
	private MListNode<T> previous, current, next;

	/// <summary>Value() (content) of the current node.</summary>
	public T Value { get { return current.value; } }
	public void SetValue(T v) { _StateCheck(); current.value = v; }

	public MListIterator(MList<T> _list, MListNode<T> _previous, int state)
	{
		listState = state;
		list = _list;
		previous = _previous;
		current = null;
		next = _previous.Next;
	}
	public bool HasValue()
	{
		_StateCheck();
		return current != null;
	}
	public bool HasNext()
	{
		_StateCheck();
		return next != null;
	}
	/// <summary>Step to next node. Return true if there was next.</summary>
	public bool Next()
	{
		_StateCheck();
		if (next == null) return false;
		if (current != null) previous = current; // current is null at the beginning and after Remove
		current = next;
		next = current.Next;
		return true;
	}
	/// <summary>Step to next node, or first if we're at the end. True if list is not empty.</summary>
	public bool Loop()
	{
		_StateCheck();
		if (list.Size() == 0) return false;
		if (next == null)
		{
			previous = list.Head();
			current = null;
			next = previous.Next;
			AssertValid();
			bool b = Next();
			MS.Assertion(b);
			AssertValid();
			return true;
		}
		if (current != null) previous = current; // current is null at the beginning and after Remove
		current = next;
		next = current.Next;
		return true;
	}
	public void Remove()
	{
		_StateCheck();
		MS.Assertion(current != null);
		list.Remove(this);
		_StateRefresh();
	}
	public void InsertAfter(T t)
	{
		_StateCheck();
		list.InsertAfter(current, t);
		_StateRefresh();
		Next();
	}
	public void InsertBefore(T t)
	{
		_StateCheck();
		MS.Assertion(current != null); // can't be beginning or end of list, or just deleted
		previous = list.InsertAfter(previous, t); // add new node and update iterator's previous
		_StateRefresh();
	}
	public bool Finished()
	{	
		_StateCheck();
		return next == null;
	}
	public void _Skip() // internal operation to do on Remove
	{
		if (next == null)
		{
			previous.Next = null; // end of list
			next = current = previous = null;
			return;
		}
		previous.Next = next;
		current = null;
	}
	public MListNode<T> _CurrentNode()
	{
		return current;
	}
	public MListNode<T> _PreviousNode()
	{
		return previous;
	}
	private void _StateRefresh()
	{
		listState = list._State(); // do it when modified the list itself
	}
	private void _StateCheck()
	{
		if (listState != list._State()) throw new MConcurrentModificationException();
	}
	private void _StateAssert()
	{
		MS.Assertion(listState == list._State());
	}
	public void AssertValid()
	{
		_StateAssert();
		if (previous != null)
		{
			if (current == null) MS.Assertion(previous.Next == next);
			else MS.Assertion(previous.Next == current);
		}
		else if (current != null)
		{
			MS.Assertion(current.Next == next);
		}
		else
		{
			MS.Assertion(next == null);
		}
	}
}

// standard enumerator for 'foreach' e.g.
public class MListEnumerator<T> : IEnumerable<T>
{
	private MList<T> list; MListIterator<T> it;
	public MListEnumerator(MList<T> _list) { list = _list; it = list.Iterator(); }
	IEnumerator IEnumerable.GetEnumerator()	{ yield return list.GetEnumerator(); }
	IEnumerator<T> IEnumerable<T>.GetEnumerator() { while (it.Next()) yield return it.Value; }
	public T Current { get { return it.Value; } }
	public bool MoveNext() { return it.Next(); }
}


public class MList<T>
{
	private int size = 0, state = 0;
	private MListNode<T>
		head, // first, empty (dummy) node
		tail; // last node, containing real data
	
	// TODO: iterator for internal use to prevent 'new' calls

	public MList() { head = new MListNode<T>(); tail = null; }
	public MListEnumerator<T> GetEnumerator() {	return new MListEnumerator<T>(this); }
	public MListIterator<T> Iterator() { return new MListIterator<T>(this, head, state); }
	public T First() { return head.Next.value; }
	public T Last()	{ return tail.value; }
	public int Size() { return size; }
	public bool IsEmpty() { return size == 0; }
	public int _State() { return state; }
	public MListNode<T> Head() { return head; } // TODO: protect, for the iterator only
	public void AddFirst(T t)
	{
		state++;
		if (Size() == 0)
		{
			AddLast(t);
			return;
		}
		var it = Iterator();
		it.Next();
		it.InsertBefore(t);
	}
	public void Add(T t)
	{
		AddLast(t);
	}
	public void AddLast(T t)
	{
		state++;
		MListNode<T> node = new MListNode<T> { value = t };
		if (size == 0) head.Next = node;
		else tail.Next = node;
		tail = node;
		size++;
	}
	public void RemoveFirst()
	{
		state++;
		if (size == 0) return;
		if (size == 1) head.Next = tail = null;
		else head.Next = head.Next.Next;
		size--;
	}
	//public void RemoveLast()
	//{
	//	// Difficult because new tail can't be defined
	//}
	public void RemoveAll()
	{
		while (size > 0) RemoveFirst();
	}
	public void Remove(MListIterator<T> it)
	{
		state++;
		if (it._CurrentNode() == tail)
		{
			tail = it._PreviousNode();
		}
		it._Skip();
		size--;
		if (size == 0) head.Next = tail = null;
	}
	public MListIterator<T> Iterator(T value)
	{
		var it = Iterator();
		while (it.Next())
		{
			if (it.Value.Equals(value)) return it;
		}
		return null;
	}
	public void RemoveEqual(T value)
	{
		var it = Iterator();
		while (it.Next())
		{
			if (it.Value.Equals(value)) it.Remove();
		}
	}
	public int EqualIndex(T value, int minIndex = 0)
	{
		var it = Iterator();
		int index = 0;
		while (it.Next())
		{
			if (it.Value.Equals(value) && index >= minIndex) return index;
			index++;
		}
		return -1;
	}
	public MListNode<T> InsertAfter(MListNode<T> position, T t)
	{
		// add a node after 'position', which can't be null
		// special cases: first, only one, last
		MS.Assertion(position != null);
		state++;
		MListNode<T> node = new MListNode<T> { value = t, Next = position.Next };
		position.Next = node;
		if (tail == position) tail = node;
		size++;
		return node;
	}
	public T GetAt(int index)
	{
		MS.Assertion(index >= 0 && index < size);
		var it = Iterator();
		while (index-- >= 0) it.Next();
		return it.Value;
	}
	public void RemoveAt(int index)
	{
		MS.Assertion(index >= 0 && index < size);
		var it = Iterator();
		while (index-- >= 0) it.Next();
		it.Remove();
	}
	public void AssertValid()
	{
		if (size == 0)
		{
			MS.Assertion(tail == null);
			MS.Assertion(head.Next == null);
		}
		else
		{
			int n = 0;
			var it = Iterator();
			while (it.Next())
			{
				it.AssertValid();
				n++;
			}
			MS.Assertion(it.Value.Equals(tail.value));
			MS.Assertion(n == size);
			MS.Assertion(it.Finished());
		}
	}
}
}