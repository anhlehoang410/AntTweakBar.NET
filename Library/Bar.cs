using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace AntTweakBar
{
    /// <summary>
    /// An AntTweakBar bar, which holds a set of variables.
    /// </summary>
    public class Bar : IEnumerable<Variable>, IDisposable
    {
        /// <summary>
        /// The default label for unnamed bars.
        /// </summary>
        private const String UnnamedLabel = "<unnamed>";

        /// <summary>
        /// Gets this bar's context-dependent unique identifier.
        /// </summary>
        internal String ID { get; private set; }

        /// <summary>
        /// Gets this bar's unmanaged AntTweakBar pointer.
        /// </summary>
        internal IntPtr Pointer { get; private set; }

        /// <summary>
        /// Gets this bar's parent context.
        /// </summary>
        public Context ParentContext { get; private set; }

        /// <summary>
        /// Creates a new bar in a a given AntTweakBar context.
        /// </summary>
        /// <param name="parent">The context the bar should be created in.</param>
        /// <param name="def">An optional definition string for the new bar.</param>
        public Bar(Context parent, String def = null)
        {
            if ((ParentContext = parent) == null)
                throw new ArgumentNullException("parent");
            
            TW.SetCurrentWindow(ParentContext.Identifier); // per context
            Pointer = TW.NewBar(ID = Guid.NewGuid().ToString());
            ParentContext.Add(this);
            Label = UnnamedLabel;
            SetDefinition(def);
        }

        /// <summary>
        /// Sets this bar's properties from a definition string.
        /// </summary>
        /// <param name="def">An AntTweakBar definition string, excluding the name prefix.</param>
        public void SetDefinition(String def)
        {
            if (def != null)
            {
                TW.SetCurrentWindow(ParentContext.Identifier);
                TW.Define(String.Format("{0} {1}", ID, def));
            }
        }

        #region Customization

        /// <summary>
        /// Shows or hides a variable group in this bar.
        /// </summary>
        /// <param name="groupName">The name of the group to show or hide.</param>
        /// <param name="visible">Whether the group should be visible.</param>
        public void ShowGroup(String groupName, Boolean visible)
        {
            TW.SetCurrentWindow(ParentContext.Identifier);
            TW.Define(String.Format("{0}/{1} visible={2}", ID, groupName, visible ? "true" : "false"));
        }

        /// <summary>
        /// Opens or closes a variable group in this bar.
        /// </summary>
        /// <param name="groupName">The name of the group to open or close.</param>
        /// <param name="opened">Whether the group should be open.</param>
        public void OpenGroup(String groupName, Boolean opened)
        {
            TW.SetCurrentWindow(ParentContext.Identifier);
            TW.Define(String.Format("{0}/{1} opened={2}", ID, groupName, opened ? "true" : "false"));
        }

        /// <summary>
        /// Gets or sets this bar's label.
        /// </summary>
        public String Label
        {
            get { return TW.GetStringParam(Pointer, null, "label"); }
            set { TW.SetParam(Pointer, null, "label", value); }
        }

        /// <summary>
        /// Gets or sets this bar's help text.
        /// </summary>
        public String Help
        {
            get { return TW.GetStringParam(Pointer, null, "help"); }
            set { TW.SetParam(Pointer, null, "help", value); }
        }

        /// <summary>
        /// Gets or sets this bar's color.
        /// </summary>
        public Color Color
        {
            get { return TW.GetColorParam(Pointer, null, "color"); }
            set { TW.SetParam(Pointer, null, "color", value); }
        }

        /// <summary>
        /// Gets or sets this bar's alpha value (opacity).
        /// </summary>
        public byte Alpha
        {
            get { return (byte)TW.GetIntParam(Pointer, null, "alpha")[0]; }
            set { TW.SetParam(Pointer, null, "alpha", value); }
        }

        /// <summary>
        /// Gets or sets this bar's position.
        /// </summary>
        public Point Position
        {
            get { return TW.GetPointParam(Pointer, null, "position"); }
            set { TW.SetParam(Pointer, null, "position", value); }
        }

        /// <summary>
        /// Gets or sets this bar's size.
        /// </summary>
        public Size Size
        {
            get { return TW.GetSizeParam(Pointer, null, "size"); }
            set { TW.SetParam(Pointer, null, "size", value); }
        }

        /// <summary>
        /// Gets or sets whether this bar can be iconified by the user.
        /// </summary>
        public Boolean Iconifiable
        {
            get { return TW.GetBooleanParam(Pointer, null, "iconifiable"); }
            set { TW.SetParam(Pointer, null, "iconifiable", value); }
        }

        /// <summary>
        /// Gets or sets whether this bar can be moved by the user.
        /// </summary>
        public Boolean Movable
        {
            get { return TW.GetBooleanParam(Pointer, null, "movable"); }
            set { TW.SetParam(Pointer, null, "movable", value); }
        }

        /// <summary>
        /// Gets or sets whether this bar can be resized by the user.
        /// </summary>
        public Boolean Resizable
        {
            get { return TW.GetBooleanParam(Pointer, null, "resizable"); }
            set { TW.SetParam(Pointer, null, "resizable", value); }
        }

        /// <summary>
        /// Gets or sets whether this bar is constrained to the window.
        /// </summary>
        public Boolean Contained
        {
            get { return TW.GetBooleanParam(Pointer, null, "contained"); }
            set { TW.SetParam(Pointer, null, "contained", value); }
        }

        /// <summary>
        /// Gets or sets whether this bar is visible.
        /// </summary>
        public Boolean Visible
        {
            get { return TW.GetBooleanParam(Pointer, null, "visible"); }
            set { TW.SetParam(Pointer, null, "visible", value); }
        }

        /// <summary>
        /// Brings this bar in front of all others.
        /// </summary>
        public void BringToFront()
        {
            TW.SetTopBar(Pointer);
        }

        /// <summary>
        /// Sends this bar behind all others.
        /// </summary>
        public void SendToBack()
        {
            TW.SetBottomBar(Pointer);
        }

        #endregion

        #region IEnumerable

        private readonly ICollection<Variable> variables = new HashSet<Variable>();

        internal void Add(Variable variable)
        {
            variables.Add(variable);
        }

        internal void Remove(Variable variable)
        {
            variables.Remove(variable);
        }

        public IEnumerator<Variable> GetEnumerator()
        {
            return variables.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region IDisposable

        ~Bar()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed && (ParentContext != null))
            {
                while (disposing && variables.Any()) {
                    variables.First().Dispose();
                }

                if (disposing && ParentContext.Contains(this)) {
                    ParentContext.Remove(this);
                }
                
                if (Pointer != null) {
                    TW.DeleteBar(Pointer);
                }

                disposed = true;
            }
        }

        private bool disposed = false;

        #endregion

        #region Misc.

        public override String ToString()
        {
            return String.Format("[Bar: {0} variable(s), Label={1}]", variables.Count, Label);
        }

        #endregion
    }
}
