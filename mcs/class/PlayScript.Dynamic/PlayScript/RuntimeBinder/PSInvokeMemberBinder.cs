//
// Copyright 2013 Zynga Inc.
//	
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//		
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.

#if !DYNAMIC_SUPPORT

using System;
using System.Collections.Generic;
using System.Diagnostics;
using PlayScript.Expando;
using System.Reflection;

namespace PlayScript.RuntimeBinder
{
	class PSInvokeMemberBinder : CallSiteBinder
	{
		private static Dictionary<Type, object> invokeTargets = new Dictionary<Type, object>();
		private static Dictionary<Type, object> hasOwnPropertyInvokeTargets = new Dictionary<Type, object>();

//		readonly CSharpBinderFlags flags;
//		readonly IList<CSharpArgumentInfo> argumentInfo;
//		readonly IList<Type> typeArguments;
//		readonly Type callingContext;

		public const int MAX_ARGS = 8;

		readonly string name;
		public PSInvokeMemberBinder (CSharpBinderFlags flags, string name, Type callingContext, IEnumerable<Type> typeArguments, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
			this.name = name;

			var argList = argumentInfo as System.Collections.IList;
			if (argList != null)
			{
				// allocate argument list
				this.mArgs = new object[argList.Count - 1];
			}

//			this.flags = flags;
//			this.callingContext = callingContext;
//			this.argumentInfo = new List<CSharpArgumentInfo>(argumentInfo);
//			if (typeArguments != null) this.typeArguments = new List<Type>(typeArguments);
		}


		// information about the current binding
		private Type              mType;
		private object[]   		  mArgs;
		private object[]   		  mConvertedArgs;
		private bool 			  mIsOverloaded;
		private MethodBinder      mMethod;
		private MethodBinder[]    mMethodList;
		private Delegate   	      mDelegate;


		private void SelectMethod(object o, int argCount)
		{
			mDelegate = null;

			// if only one method, then use it
			if (mMethodList.Length == 1) {
				mMethod       = mMethodList[0];
				mIsOverloaded = false;
				return;
			}

			mIsOverloaded = true;

			// select method based on simple compatibility
			// TODO: this could change to use a rating system
			foreach (var method in mMethodList) {
				// is this method compatible?
				if (method.CheckArguments(mArgs)) {
					mMethod = method;
					return;
				}
			}

			// no methods compatible?

			// try to get method from dynamic class
			var dc = o as IDynamicClass;
			if (dc != null) {
				var func = dc.__GetDynamicValue(this.name) as Delegate;
				if (func != null) {
					// use function
					mDelegate = func;
					return;
				}
			}

			throw new InvalidOperationException("Could not find suitable method to invoke: " + this.name + " for type: " + mType);
		}

		private object ResolveAndInvoke(CallSite site, object o, int argCount)
		{
			// determine object type
			Type otype;
			bool isStatic;
			if (o is Type) {
				// this is a static method invocation where o is the class
				otype = (Type)o;
				isStatic = true;
			} else {
				// this is a instance method invocation
				otype = o.GetType();
				isStatic = false;
			}

			// see if type has changed
			if (otype != mType)
			{
				// re-resolve method list if type has changed
				mType           = otype;

				// get method list for type and method name
				BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public;
				if (isStatic) {
					flags|=BindingFlags.Static;
				} else {
					flags|=BindingFlags.Instance;
				}

				mMethodList     = MethodBinder.LookupMethodList(mType, this.name, flags, argCount);

				// select new method to use
				SelectMethod(o, argCount);
			}
			else if (mIsOverloaded)	
			{
				// if there are overloads we select the method every time
				// we could look into a more optimal way of doing this if it becomes a problem
				SelectMethod(o, argCount);
			}

			if (mDelegate == null)
			{
				// invoke as method
				// convert arguments for method
				if (mMethod.ConvertArguments(o, mArgs, argCount, ref mConvertedArgs)) {
					// invoke method with converted arguments
					return mMethod.Method.Invoke(o, mConvertedArgs);
				}
			}
			else
			{
				// invoke as delegate
				// convert arguments for delegate and invoke
				object[] newargs = PlayScript.Dynamic.ConvertArgumentList(mDelegate.Method, mArgs);
				return mDelegate.DynamicInvoke(newargs);
			}

			throw new InvalidOperationException("Could not find suitable method to invoke: " + this.name + " for type: " + mType);
		}

		private static void InvokeAction0 (CallSite site, object o)
		{
#if BINDERS_RUNTIME_STATS
			++Stats.CurrentInstance.InvokeMemberBinderInvoked;
#endif
			var binder = (PSInvokeMemberBinder)site.Binder;
			binder.ResolveAndInvoke(site, o, 0);
		}

		private static void InvokeAction1 (CallSite site, object o, object a1)
		{
#if BINDERS_RUNTIME_STATS
			++Stats.CurrentInstance.InvokeMemberBinderInvoked;
#endif
			var binder = (PSInvokeMemberBinder)site.Binder;
			var args   = binder.mArgs;
			args[0] = a1;
			binder.ResolveAndInvoke(site, o, 1);
		}

		private static void InvokeAction2 (CallSite site, object o, object a1, object a2)
		{
#if BINDERS_RUNTIME_STATS
			++Stats.CurrentInstance.InvokeMemberBinderInvoked;
#endif
			var binder = (PSInvokeMemberBinder)site.Binder;
			var args   = binder.mArgs;
			args[0] = a1;
			args[1] = a2;
			binder.ResolveAndInvoke(site, o, 2);
		}

		private static void InvokeAction3 (CallSite site, object o, object a1, object a2, object a3)
		{
#if BINDERS_RUNTIME_STATS
			++Stats.CurrentInstance.InvokeMemberBinderInvoked;
#endif
			var binder = (PSInvokeMemberBinder)site.Binder;
			var args   = binder.mArgs;
			args[0] = a1;
			args[1] = a2;
			args[2] = a3;
			binder.ResolveAndInvoke(site, o, 3);
		}

		private static void InvokeAction4 (CallSite site, object o, object a1, object a2, object a3, object a4)
		{
#if BINDERS_RUNTIME_STATS
			++Stats.CurrentInstance.InvokeMemberBinderInvoked;
#endif
			var binder = (PSInvokeMemberBinder)site.Binder;
			var args   = binder.mArgs;
			args[0] = a1;
			args[1] = a2;
			args[2] = a3;
			args[3] = a4;
			binder.ResolveAndInvoke(site, o, 4);
		}

		private static void InvokeAction5 (CallSite site, object o, object a1, object a2, object a3, object a4, object a5)
		{
#if BINDERS_RUNTIME_STATS
			++Stats.CurrentInstance.InvokeMemberBinderInvoked;
#endif
			var binder = (PSInvokeMemberBinder)site.Binder;
			var args   = binder.mArgs;
			args[0] = a1;
			args[1] = a2;
			args[2] = a3;
			args[3] = a4;
			args[4] = a5;
			binder.ResolveAndInvoke(site, o, 5);
		}

		private static void InvokeAction6 (CallSite site, object o, object a1, object a2, object a3, object a4, object a5, object a6)
		{
#if BINDERS_RUNTIME_STATS
			++Stats.CurrentInstance.InvokeMemberBinderInvoked;
#endif
			var binder = (PSInvokeMemberBinder)site.Binder;
			var args   = binder.mArgs;
			args[0] = a1;
			args[1] = a2;
			args[2] = a3;
			args[3] = a4;
			args[4] = a5;
			args[5] = a6;
			binder.ResolveAndInvoke(site, o, 6);
		}

		private static void InvokeAction7 (CallSite site, object o, object a1, object a2, object a3, object a4, object a5, object a6, object a7)
		{
#if BINDERS_RUNTIME_STATS
			++Stats.CurrentInstance.InvokeMemberBinderInvoked;
#endif
			var binder = (PSInvokeMemberBinder)site.Binder;
			var args   = binder.mArgs;
			args[0] = a1;
			args[1] = a2;
			args[2] = a3;
			args[3] = a4;
			args[4] = a5;
			args[5] = a6;
			args[6] = a7;
			binder.ResolveAndInvoke(site, o, 7);
		}

		
		private static void InvokeAction8 (CallSite site, object o, object a1, object a2, object a3, object a4, object a5, object a6, object a7, object a8)
		{
#if BINDERS_RUNTIME_STATS
			++Stats.CurrentInstance.InvokeMemberBinderInvoked;
#endif
			var binder = (PSInvokeMemberBinder)site.Binder;
			var args   = binder.mArgs;
			args[0] = a1;
			args[1] = a2;
			args[2] = a3;
			args[3] = a4;
			args[4] = a5;
			args[5] = a6;
			args[6] = a7;
			args[7] = a8;
			binder.ResolveAndInvoke(site, o, 8);
		}

		private static object InvokeFunc0 (CallSite site, object o)
		{
#if BINDERS_RUNTIME_STATS
			++Stats.CurrentInstance.InvokeMemberBinderInvoked;
#endif
			var binder = (PSInvokeMemberBinder)site.Binder;
			return binder.ResolveAndInvoke(site, o, 0);
		}
		
		private static object InvokeFunc1 (CallSite site, object o, object a1)
		{
#if BINDERS_RUNTIME_STATS
			++Stats.CurrentInstance.InvokeMemberBinderInvoked;
#endif
			var binder = (PSInvokeMemberBinder)site.Binder;
			var args   = binder.mArgs;
			args[0] = a1;
			return binder.ResolveAndInvoke(site, o, 1);
		}
		
		private static object InvokeFunc2 (CallSite site, object o, object a1, object a2)
		{
#if BINDERS_RUNTIME_STATS
			++Stats.CurrentInstance.InvokeMemberBinderInvoked;
#endif
			var binder = (PSInvokeMemberBinder)site.Binder;
			var args   = binder.mArgs;
			args[0] = a1;
			args[1] = a2;
			return binder.ResolveAndInvoke(site, o, 2);
		}
		
		private static object InvokeFunc3 (CallSite site, object o, object a1, object a2, object a3)
		{
#if BINDERS_RUNTIME_STATS
			++Stats.CurrentInstance.InvokeMemberBinderInvoked;
#endif
			var binder = (PSInvokeMemberBinder)site.Binder;
			var args   = binder.mArgs;
			args[0] = a1;
			args[1] = a2;
			args[2] = a3;
			return binder.ResolveAndInvoke(site, o, 3);
		}
		
		private static object InvokeFunc4 (CallSite site, object o, object a1, object a2, object a3, object a4)
		{
#if BINDERS_RUNTIME_STATS
			++Stats.CurrentInstance.InvokeMemberBinderInvoked;
#endif
			var binder = (PSInvokeMemberBinder)site.Binder;
			var args   = binder.mArgs;
			args[0] = a1;
			args[1] = a2;
			args[2] = a3;
			args[3] = a4;
			return binder.ResolveAndInvoke(site, o, 4);
		}

		
		private static object InvokeFunc5 (CallSite site, object o, object a1, object a2, object a3, object a4, object a5)
		{
#if BINDERS_RUNTIME_STATS
			++Stats.CurrentInstance.InvokeMemberBinderInvoked;
#endif
			var binder = (PSInvokeMemberBinder)site.Binder;
			var args   = binder.mArgs;
			args[0] = a1;
			args[1] = a2;
			args[2] = a3;
			args[3] = a4;
			args[4] = a5;
			return binder.ResolveAndInvoke(site, o, 5);
		}
		
		private static object InvokeFunc6 (CallSite site, object o, object a1, object a2, object a3, object a4, object a5, object a6)
		{
#if BINDERS_RUNTIME_STATS
			++Stats.CurrentInstance.InvokeMemberBinderInvoked;
#endif
			var binder = (PSInvokeMemberBinder)site.Binder;
			var args   = binder.mArgs;
			args[0] = a1;
			args[1] = a2;
			args[2] = a3;
			args[3] = a4;
			args[4] = a5;
			args[5] = a6;
			return binder.ResolveAndInvoke(site, o, 6);
		}
		
		private static object InvokeFunc7 (CallSite site, object o, object a1, object a2, object a3, object a4, object a5, object a6, object a7)
		{
#if BINDERS_RUNTIME_STATS
			++Stats.CurrentInstance.InvokeMemberBinderInvoked;
#endif
			var binder = (PSInvokeMemberBinder)site.Binder;
			var args   = binder.mArgs;
			args[0] = a1;
			args[1] = a2;
			args[2] = a3;
			args[3] = a4;
			args[4] = a5;
			args[5] = a6;
			args[6] = a7;
			return binder.ResolveAndInvoke(site, o, 7);
		}
		
		private static object InvokeFunc8 (CallSite site, object o, object a1, object a2, object a3, object a4, object a5, object a6, object a7, object a8)
		{
#if BINDERS_RUNTIME_STATS
			++Stats.CurrentInstance.InvokeMemberBinderInvoked;
#endif
			var binder = (PSInvokeMemberBinder)site.Binder;
			var args   = binder.mArgs;
			args[0] = a1;
			args[1] = a2;
			args[2] = a3;
			args[3] = a4;
			args[4] = a5;
			args[5] = a6;
			args[6] = a7;
			args[7] = a8;
			return binder.ResolveAndInvoke(site, o, 8);
		}


		// minor optimization for has own propety
		private static object InvokeFunc1_hasOwnProperty(CallSite site, object o, object a1)
		{
			if (o == null || a1 == null) return false;

			var name = a1 as string;
			if (name != null) {
				return PlayScript.Dynamic.HasOwnProperty(o, name);
			}

			// fallback
			return InvokeFunc1(site, o, a1);
		}

		
		static PSInvokeMemberBinder ()
		{
			invokeTargets.Add (typeof(Action<CallSite,object>), 
			                   (Action<CallSite,object>)InvokeAction0);
			invokeTargets.Add (typeof(Action<CallSite,object,object>), 
			                   (Action<CallSite,object,object>)InvokeAction1);
			invokeTargets.Add (typeof(Action<CallSite,object,object,object>), 
			                   (Action<CallSite,object,object,object>)InvokeAction2);
			invokeTargets.Add (typeof(Action<CallSite,object,object,object,object>), 
			                   (Action<CallSite,object,object,object,object>)InvokeAction3);
			invokeTargets.Add (typeof(Action<CallSite,object,object,object,object,object>), 
			                   (Action<CallSite,object,object,object,object,object>)InvokeAction4);
			invokeTargets.Add (typeof(Action<CallSite,object,object,object,object,object,object>), 
			                   (Action<CallSite,object,object,object,object,object,object>)InvokeAction5);
			invokeTargets.Add (typeof(Action<CallSite,object,object,object,object,object,object,object>), 
			                   (Action<CallSite,object,object,object,object,object,object,object>)InvokeAction6);
			invokeTargets.Add (typeof(Action<CallSite,object,object,object,object,object,object,object,object>), 
			                   (Action<CallSite,object,object,object,object,object,object,object,object>)InvokeAction7);
			invokeTargets.Add (typeof(Action<CallSite,object,object,object,object,object,object,object,object,object>), 
			                   (Action<CallSite,object,object,object,object,object,object,object,object,object>)InvokeAction8);
			invokeTargets.Add (typeof(Func<CallSite,object,object>), 
			                   (Func<CallSite,object,object>)InvokeFunc0);
			invokeTargets.Add (typeof(Func<CallSite,object,object,object>), 
			                   (Func<CallSite,object,object,object>)InvokeFunc1);
			invokeTargets.Add (typeof(Func<CallSite,object,object,object,object>), 
			                   (Func<CallSite,object,object,object,object>)InvokeFunc2);
			invokeTargets.Add (typeof(Func<CallSite,object,object,object,object,object>), 
			                   (Func<CallSite,object,object,object,object,object>)InvokeFunc3);
			invokeTargets.Add (typeof(Func<CallSite,object,object,object,object,object,object>), 
			                   (Func<CallSite,object,object,object,object,object,object>)InvokeFunc4);
			invokeTargets.Add (typeof(Func<CallSite,object,object,object,object,object,object,object>),
			                   (Func<CallSite,object,object,object,object,object,object,object>)InvokeFunc5);
			invokeTargets.Add (typeof(Func<CallSite,object,object,object,object,object,object,object,object>), 
			                   (Func<CallSite,object,object,object,object,object,object,object,object>)InvokeFunc6);
			invokeTargets.Add (typeof(Func<CallSite,object,object,object,object,object,object,object,object,object>), 
			                   (Func<CallSite,object,object,object,object,object,object,object,object,object>)InvokeFunc7);
			invokeTargets.Add (typeof(Func<CallSite,object,object,object,object,object,object,object,object,object,object>), 
			                   (Func<CallSite,object,object,object,object,object,object,object,object,object,object>)InvokeFunc8);

			hasOwnPropertyInvokeTargets.Add (typeof(Func<CallSite,object,object,object>), 
			                   (Func<CallSite,object,object,object>)InvokeFunc1_hasOwnProperty);

		}

		public override object Bind (Type delegateType)
		{
			object target;
			// special case optimization here
			if (this.name == "hasOwnProperty") {
				if (hasOwnPropertyInvokeTargets.TryGetValue(delegateType, out target))
				{
					return target;
				}
			} 

			invokeTargets.TryGetValue(delegateType, out target);
			return target;
		}
	}
}


#endif