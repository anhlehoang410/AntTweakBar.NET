using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace AntTweakBar
{
    /// <summary>
    /// An AntTweakBar variable which can hold an enum.
    /// </summary>
    public sealed class EnumVariable<T> : Variable where T : struct
    {
        /// <summary>
        /// Occurs when the user changes this variable's value.
        /// </summary>
        public event EventHandler Changed;

        /// <summary>
        /// Raises the Changed event.
        /// </summary>
        public void OnChanged(EventArgs e)
        {
            ThrowIfDisposed();

            if (Changed != null) {
                Changed(this, e);
            }
        }

        /// <summary>
        /// Gets or sets the value of this variable.
        /// </summary>
        public T Value
        {
            get { ThrowIfDisposed(); return value; }
            set
            {
                ThrowIfDisposed();

                if (!Enum.IsDefined (typeof(T), value)) {
                    throw new ArgumentOutOfRangeException("value", "Invalid variable value");
                } else {
                    this.value = value;
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private T value;

        /// <summary>
        /// Initialization delegate, which creates the enum variable.
        /// </summary>
        private static void InitEnumVariable(Variable var, String id)
        {
            if (!typeof(T).IsEnum) {
                throw new InvalidOperationException(String.Format("Type {0} is not an enumeration", typeof(T).FullName));
            }

            var enumNames = String.Join(",", typeof(T).GetEnumNames());

            var it = var as EnumVariable<T>;

            Tw.SetCallbacks.Add(id, new Tw.SetVarCallback(it.SetCallback));
            Tw.GetCallbacks.Add(id, new Tw.GetVarCallback(it.GetCallback));

            Tw.AddVarCB(var.ParentBar.Pointer, id,
                        Tw.DefineEnumFromString(typeof(T).FullName, enumNames),
                        Tw.SetCallbacks[id],
                        Tw.GetCallbacks[id],
                        IntPtr.Zero, null);
        }

        /// <summary>
        /// Creates a new enum variable in a given bar.
        /// </summary>
        /// <param name="bar">The bar to create the enum variable in.</param>
        /// <param name="initialValue">The initial value of the variable.</param>
        /// <param name="def">An optional definition string for the new variable.</param>
        public EnumVariable(Bar bar, T initialValue, String def = null)
            : base(bar, InitEnumVariable, def)
        {
            Tw.SetParam(ParentBar.Pointer, ID, "enum", GetEnumString());

            Value = initialValue;
        }

        /// <summary>
        /// Called by AntTweakBar when the user changes the variable's value.
        /// </summary>
        private void SetCallback(IntPtr pointer, IntPtr clientData)
        {
            int data = Marshal.ReadInt32(pointer);
            bool changed = (data != (int)(object)Value);
            Value = (T)(object)data;

            if (changed) {
                OnChanged(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Called by AntTweakBar when AntTweakBar needs the variable's value.
        /// </summary>
        private void GetCallback(IntPtr pointer, IntPtr clientData)
        {
            Marshal.WriteInt32(pointer, (int)(object)Value);
        }

        /// <summary>
        /// Returns a formatted key-value representation of this enum.
        /// </summary>
        private static String GetEnumString()
        {
            IList<String> enumList = new List<String>();

            foreach (var kv in ((int[])Enum.GetValues(typeof(T))).Zip(typeof(T).GetEnumNames(), (i, n) => new Tuple<int, string>(i, n)))
            {
                var valueAttributes = typeof(T).GetMember(kv.Item2)[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                string label = valueAttributes.Any() ? ((DescriptionAttribute)valueAttributes.First()).Description : kv.Item2;
                enumList.Add(String.Format("{0} {{{1}}}", kv.Item1, label)); // Follows the AntTweakBar enum string format.
            }

            return String.Join(",", enumList);
        }

        public override String ToString()
        {
            return String.Format("[EnumVariable<{0}>: Label={1}, Value={2}]", typeof(T).Name, Label, Value);
        }
    }
}
