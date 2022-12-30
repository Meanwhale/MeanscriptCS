using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meanscript
{
	public class Types : MC
	{
		internal Common common;
		protected int typeIDCounter = MAX_MS_TYPES;
		internal Dictionary<int, TypeDef> types = new Dictionary<int, TypeDef>();
		public Texts texts;
		
		public Types(Texts _texts)
		{
			texts = _texts;
		}

		public int GetNewTypeID()
		{
			return typeIDCounter++;
		}
		public TypeDef AddElementaryType(int typeID, int size)
		{
			return AddTypeDef(new PrimitiveType(typeID, size));
		}
		public TypeDef AddOperatorType(int typeID, MSText name)
		{
			return AddTypeDef(new OperatorType(typeID, name));
		}
		public TypeDef AddTypeDef(TypeDef newType)
		{
			MS.Assertion(!types.ContainsKey(newType.ID));
			types[newType.ID] = newType;
			return newType;
		}

		public bool HasDataType(MSText name)
		{
			return GetDataType(name) != null;
		}

		public bool HasType(int id)
		{
			return types.ContainsKey(id);
		}

		public TypeDef GetType(int id, NodeIterator it = null)
		{
			if (types.ContainsKey(id)) return types[id];
			return null;
		}
		public TypeDef GetType(MSText name, NodeIterator it = null)
		{
			foreach(var t in types.Values)
			{
				if (t is TypeDef d)
				{
					if (name.Equals(d.TypeName())) return d;
				}
			}
			return null;
		}
		
		public DataTypeDef GetDataType(int id, NodeIterator it = null)
		{
			var t = GetType(id,it);
			if (t != null && t is DataTypeDef d) return d;
			return null;
		}
		public DataTypeDef GetDataType(MSText name, NodeIterator itPtr = null)
		{
			var t = GetType(name,itPtr);
			if (t != null && t is DataTypeDef d) return d;
			return null;
		}
		public StructDef GetStructDefType(int i, NodeIterator it = null)
		{
			return null; // TODO
		}
		public StructDef GetStructDefType(MSText name, NodeIterator it = null)
		{
			return null; // TODO
		}
		


		internal void AddCallback(TypeDef type, StructDef sd, MS.MCallbackAction act)
		{
			throw new NotImplementedException();
		}

	}
}
