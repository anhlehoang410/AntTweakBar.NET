using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;

namespace AntTweakBar
{
    /// <summary>
    /// A variable holding an integer value.
    /// </summary>
    public class IntVariable : Variable
    {
        #region Fields

        private readonly TW.GetVarCallback getCallback;
        private readonly TW.SetVarCallback setCallback;

        /// <summary>
        /// Occurs when the user changes the variable.
        /// </summary>
        public event EventHandler Changed;

        /// <summary>
        /// Raises the Changed event.
        /// </summary>
        private void OnChanged(EventArgs e)
        {
            if (Changed != null)
                Changed(this, e);
        }

        private Int32 value;

        #endregion

        public IntVariable(Bar bar, Int32 initialValue = 0, String def = null)
            : base(bar)
        {
            setCallback = SetCallback;
            getCallback = GetCallback;

            TW.SetCurrentWindow(bar.Owner.WindowIndex);
            TW.AddVarCB(Owner, ID, TW.VariableType.TW_TYPE_INT32,
                        setCallback, getCallback, IntPtr.Zero);

            Owner.Add(this);
            Label = "undef";
            if (def != null)
                SetDefinition(def);
            Value = initialValue;
        }

        /// <summary>
        /// Gets or sets the value of this variable.
        /// </summary>
        public Int32 Value
        {
            get { return value; }
            set
            {
                if (!((Min <= value) && (value <= Max)))
                    throw new ArgumentOutOfRangeException("value", "Invalid variable value");
                else
                    this.value = value;
            }
        }

        private unsafe void SetCallback(IntPtr pointer, IntPtr clientData)
        {
            int tmp = *(int*)pointer;
            bool changed = tmp != Value;
            Value = tmp;

            if (changed)
                OnChanged(EventArgs.Empty);
        }

        private unsafe void GetCallback(IntPtr pointer, IntPtr clientData)
        {
            *(int*)pointer = Value;
        }

        #region Customization

        /// <summary>
        /// Gets or sets the minimum value of this variable.
        /// </summary>
        public Int32 Min
        {
            get { return TW.GetIntParam(Owner, ID, "min")[0]; }
            set
            {
                TW.SetParam(Owner, ID, "min", value);
                Value = Math.Max(value, Value);
            }
        }

        /// <summary>
        /// Gets or sets the maximum value of this variable.
        /// </summary>
        public Int32 Max
        {
            get { return TW.GetIntParam(Owner, ID, "max")[0]; }
            set
            {
                TW.SetParam(Owner, ID, "max", value);
                Value = Math.Min(value, Value);
            }
        }

        /// <summary>
        /// Gets or sets whether to use hexadecimal notation.
        /// </summary>
        public Boolean Hexadecimal
        {
            get { return TW.GetBooleanParam(Owner, ID, "hexa"); }
            set { TW.SetParam(Owner, ID, "hexa", value); }
        }

        /// <summary>
        /// Gets or sets the step (increment) of this variable.
        /// </summary>
        public Int32 Step
        {
            get { return TW.GetIntParam(Owner, ID, "step")[0]; }
            set { TW.SetParam(Owner, ID, "step", value); }
        }

        #endregion
    }
}

