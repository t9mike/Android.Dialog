﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;

namespace Android.Dialog
{
    public class RootElement : Element, IEnumerable<Section>, IDialogInterfaceOnClickListener
    {
        TextView _caption;
        TextView _value;


        internal Group _group;
        public bool UnevenRows;
        public Func<RootElement, View> _createOnSelected;
        public event EventHandler RadioSelectionChanged;

        /// <summary>
        ///  Initializes a RootSection with a caption
        /// </summary>
        /// <param name="caption">
        ///  The caption to render.
        /// </param>
        public RootElement(string caption)
            : base(caption, (int)DroidResources.ElementLayout.dialog_root)
        {
            Sections = new List<Section>();
        }

        /// <summary>
        /// Initializes a RootSection with a caption and a callback that will
        /// create the nested UIViewController that is activated when the user
        /// taps on the element.
        /// </summary>
        /// <param name="caption">
        ///  The caption to render.
        /// </param>
        public RootElement(string caption, Func<RootElement, View> createOnSelected)
            : base(caption, (int)DroidResources.ElementLayout.dialog_root)
        {
            this._createOnSelected = createOnSelected;
            Sections = new List<Section>();
        }

        /// <summary>
        ///   Initializes a RootElement with a caption with a summary fetched from the specified section and leement
        /// </summary>
        /// <param name="caption">
        /// The caption to render cref="System.String"/>
        /// </param>
        /// <param name="section">
        /// The section that contains the element with the summary.
        /// </param>
        /// <param name="element">
        /// The element index inside the section that contains the summary for this RootSection.
        /// </param>
        public RootElement(string caption, int section, int element)
            : base(caption, (int)DroidResources.ElementLayout.dialog_root)
        {
        }

        /// <summary>
        /// Initializes a RootElement that renders the summary based on the radio settings of the contained elements. 
        /// </summary>
        /// <param name="caption">
        /// The caption to ender
        /// </param>
        /// <param name="group">
        /// The group that contains the checkbox or radio information.  This is used to display
        /// the summary information when a RootElement is rendered inside a section.
        /// </param>
        public RootElement(string caption, Group group)
            : base(caption, (int)DroidResources.ElementLayout.dialog_root)
        {
            this._group = group;
        }

        /// <summary>
        /// Single save point for a context, elements can get this context via GetContext() for navigation operations
        /// </summary>
        public Context Context { get; set; }

        internal List<Section> Sections = new List<Section>();

        public int Count
        {
            get
            {
                return Sections.Count;
            }
        }

        public Section this[int idx]
        {
            get
            {
                return Sections[idx];
            }
        }

        public event EventHandler ValueChanged;

        private void HandleValueChangedEvent(object sender, EventArgs args)
        {
            if (ValueChanged != null)
                ValueChanged(sender, args);
        }

        internal int IndexOf(Section target)
        {
            int idx = 0;
            foreach (Section s in Sections)
            {
                if (s == target)
                    return idx;
                idx++;
            }
            return -1;
        }

        internal void Prepare()
        {
            int current = 0;
            foreach (var element in Sections.SelectMany(s => s))
            {
                var re = element as RadioElement;
                if (re != null)
                    re.RadioIdx = current++;
                if (UnevenRows == false && element is IElementSizing)
                    UnevenRows = true;
            }
        }

        public override string Summary()
        {
            return GetSelectedValue();
        }

        /// <summary>
        /// Adds a new section to this RootElement
        /// </summary>
        /// <param name="section">
        /// The section to add, if the root is visible, the section is inserted with no animation
        /// </param>
        public void Add(Section section)
        {
            if (section == null)
                return;

            Sections.Add(section);
            section.Parent = this;
            section.ValueChanged += HandleValueChangedEvent;
        }

        //
        // This makes things LINQ friendly;  You can now create RootElements
        // with an embedded LINQ expression, like this:
        // new RootElement ("Title") {
        //     from x in names
        //         select new Section (x) { new StringElement ("Sample") }
        //
        public void Add(IEnumerable<Section> sections)
        {
            foreach (var s in sections)
                Add(s);
        }

        /// <summary>
        /// Inserts a new section into the RootElement
        /// </summary>
        /// <param name="idx">
        /// The index where the section is added <see cref="System.Int32"/>
        /// </param>
        /// <param name="newSections">
        /// A <see cref="Section[]"/> list of sections to insert
        /// </param>
        /// <remarks>
        ///    This inserts the specified list of sections (a params argument) into the
        ///    root using the specified animation.
        /// </remarks>
        public void Insert(int idx, params Section[] newSections)
        {
            if (idx < 0 || idx > Sections.Count)
                return;
            if (newSections == null)
                return;

            //if (Table != null)
            //    Table.BeginUpdates();

            int pos = idx;
            foreach (var s in newSections)
            {
                s.Parent = this;
                s.ValueChanged += HandleValueChangedEvent;
                Sections.Insert(pos++, s);
            }
        }

        /// <summary>
        /// Removes a section at a specified location
        /// </summary>
        public void RemoveAt(int idx)
        {
            if (idx < 0 || idx >= Sections.Count)
                return;

            Sections.RemoveAt(idx);
        }

        public void Remove(Section s)
        {
            if (s == null)
                return;
            int idx = Sections.IndexOf(s);
            if (idx == -1)
                return;
            RemoveAt(idx);
        }

        public void Clear()
        {
            foreach (var s in Sections)
                s.Dispose();
            Sections = new List<Section>();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Context = null;
                if (Sections == null)
                    return;
                Clear();
                Sections = null;
            }
        }

        /// <summary>
        /// The currently selected Radio item in the whole Root.
        /// </summary>
        public int RadioSelected
        {
            get
            {
                var radio = _group as RadioGroup;
                if (radio != null)
                    return radio.Selected;
                return -1;
            }
            set
            {
                var radio = _group as RadioGroup;
                if (radio != null)
                    radio.Selected = value;
            }
        }

        private string GetSelectedValue()
        {
            var radio = _group as RadioGroup;
            if (radio == null)
                return string.Empty;

            int selected = radio.Selected;
            int current = 0;
            foreach (RadioElement e in Sections.SelectMany(s => s).OfType<RadioElement>())
            {
                if (current == selected)
                    return e.Summary();

                current++;
            }

            return string.Empty;
        }

        public override View GetView(Context context, View convertView, ViewGroup parent)
        {
            Context = context;

            View cell = new TextView(context) { TextSize = 16f, Text = Caption };
            var radio = _group as RadioGroup;

            if (radio != null)
            {
                var radioValue = GetSelectedValue();
                cell = DroidResources.LoadStringElementLayout(context, convertView, parent, LayoutId, out _caption, out _value);
                if (cell != null)
                {
                    _caption.Text = Caption;
                    _value.Text = radioValue;
                    Click = (o, e) => SelectRadio();
                }
            }
            //else if (_group != null)
            //{
            //    int count = 0;
            //    foreach (var s in Sections)
            //    {
            //        foreach (var e in s.Elements)
            //        {
            //            var ce = e as CheckboxElement;
            //            if (ce != null)
            //            {
            //                if (ce.Value)
            //                    count++;
            //                continue;
            //            }
            //            var be = e as BoolElement;
            //            if (be == null) continue;
            //            if (be.Value)
            //                count++;
            //        }
            //    }
            //    cell.DetailTextLabel.Text = count.ToString();
            //}
            //else if (_summarySection != -1 && _summarySection < Sections.Count)
            //{
            //    var s = Sections[_summarySection];
            //    if (summaryElement < s.Elements.Count)
            //        cell.DetailTextLabel.Text = s.Elements[summaryElement].Summary();
            //}
            //cell.Accessory = UITableViewCellAccessory.DisclosureIndicator;

            return cell;
        }


        public void SelectRadio()
        {
            var dialog = new AlertDialog.Builder(Context);
            dialog.SetSingleChoiceItems(Sections.SelectMany(s => s).OfType<RadioElement>().Select(e => e.Summary()).ToArray(), RadioSelected, this);
            dialog.SetTitle(Caption);
            dialog.SetNegativeButton("Cancel", this);
            dialog.Create().Show();
        }

        void IDialogInterfaceOnClickListener.OnClick(IDialogInterface dialog, int which)
        {
            if (which >= 0 && RadioSelected != which)
            {
                RadioSelected = which;
                var radioValue = GetSelectedValue();
                _value.Text = radioValue;

                if (RadioSelectionChanged != null)
                    RadioSelectionChanged(this, EventArgs.Empty);
            }

            dialog.Dismiss();
        }

        /// <summary>
        /// Enumerator that returns all the sections in the RootElement.
        /// </summary>
        /// <returns>
        /// A <see cref="IEnumerator"/>
        /// </returns>
        public IEnumerator<Section> GetEnumerator()
        {
            return Sections.GetEnumerator();
        }

        /// <summary>
        /// Enumerator that returns all the sections in the RootElement.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return Sections.GetEnumerator();
        }
    }
}