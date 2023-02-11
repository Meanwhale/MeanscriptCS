using System.Collections.Generic;

namespace Meanscript.Core
{
	public abstract class ITypes
	{
		// base class for basic and common types.
		// container for types and callbacks.

		internal Dictionary<int,CallbackType> callbacks = new Dictionary<int, CallbackType>();
		internal Dictionary<int, TypeDef> types = new Dictionary<int, TypeDef>();
		
		public void CreateCallback(int id, ArgType returnType, ArgType [] args, MS.MCallbackAction _func)
		{
			// StructDefin types = null. Ongelma? Pitäisikö tehdä minimaaliset callbackit tässä vaiheessa?
			// niitä tarvitaan myöhemmin joka tapauksessa.

			var sd = new StructDef(null, 0); // new StructDef(types, 0);
			foreach(var arg in args) sd.AddMember(arg);
			var cbTypeDef = 
				new CallbackType(
					id,
					returnType,
					sd,
					_func
			);
			MS.Assertion(!types.ContainsKey(cbTypeDef.ID));
			types[cbTypeDef.ID] = cbTypeDef;
			callbacks[id] = cbTypeDef;
		}
		public void CreateCallback(int id, ArgType returnType, int argsSize, MS.MCallbackAction _func)
		{
			// minimaalinen callback

			var cbTypeDef = 
				new CallbackType(
					id,
					returnType,
					argsSize,
					_func
			);
			
			MS.Assertion(!types.ContainsKey(cbTypeDef.ID));
			types[cbTypeDef.ID] = cbTypeDef;
			callbacks[id] = cbTypeDef;
		}
		internal virtual CallbackType FindCallback(MList<ArgType> args)
		{
			foreach (var cb in callbacks.Values)
			{
				if (cb.argStruct.Match(args)) return cb;
			}
			return null;
		}
	}

	public class CodeTypes : ITypes
	{
		protected int typeIDCounter = MC.FIRST_CUSTOM_TYPE_ID;
		public Texts texts;
		
		public CodeTypes(Texts _texts)
		{
			texts = _texts;
		}

		public int GetNewTypeID()
		{
			return typeIDCounter++;
		}
		
		public TypeDef AddTypeDef(TypeDef newType)
		{
			MS.Assertion(newType.ID > MC.MAX_BASIC_TYPES);
			MS.Assertion(!types.ContainsKey(newType.ID));
			types[newType.ID] = newType;
			return newType;
		}

		// accessors: access common and custom types. add common (basic) types first.

		public bool HasDataType(MSText name)
		{
			return GetDataType(name) != null;
		}

		public bool HasType(int id)
		{
			if (id < MC.MAX_BASIC_TYPES)
			{
				return MC.basics.HasBasicType(id);
			}
			return types.ContainsKey(id);
		}

		public TypeDef GetType(int id, NodeIterator it = null)
		{
			if (id < MC.MAX_BASIC_TYPES)
			{
				if (MC.basics.types.ContainsKey(id)) return MC.basics.types[id];
				return null;
			}
			if (types.ContainsKey(id)) return types[id];
			return null;
		}
		public TypeDef GetType(MSText name, NodeIterator it = null)
		{
			var bt = MC.basics.GetBasicType(name, it);
			if (bt != null) return bt;

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
		public DataTypeDef GetDataType(MSText name, NodeIterator it = null)
		{
			var t = GetType(name,it);
			if (t != null && t is DataTypeDef d) return d;
			return null;
		}
		internal CallbackType GetCallback(int id)
		{
			if (id < MC.MAX_BASIC_TYPES) return MC.basics.callbacks[id];
			return callbacks[id];
		}
		internal bool HasCallback(int id)
		{
			return MC.basics.callbacks.ContainsKey(id) || callbacks.ContainsKey(id);
		}
		internal override CallbackType FindCallback(MList<ArgType> args)
		{
			var basic = MC.basics.FindCallback(args);
			if (basic != null) return basic;

			foreach (var cb in callbacks.Values)
			{
				if (cb.argStruct != null && cb.argStruct.Match(args)) return cb;
			}
			return null;
		}
	}
}
