using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace Utils
{
    /// <summary>
    /// Static class with functions to draw a <see cref="ListView"/> control
    /// with colored column headers and an optional sort function.
    /// </summary>
    public static class CustomizedListView
    {
        private const string SortAscSymbol = "▲ ";
        private const string SortDescSymbol = "▼ ";

        /// <summary>
        /// Returns all listview items as a list of string arrays.
        /// </summary>
        /// <param name="collection">The listview item collection.</param>
        /// <returns>A list with arrays of strings.</returns>
        public static List<string[]> ToList(this ListView.ListViewItemCollection collection)
        {
            var result = new List<string[]>();

            foreach (ListViewItem item in collection)
            {
                var subItems = new string[item.SubItems.Count];

                for (int i = 0; i < subItems.Length; i++)
                {
                    subItems[i] = item.SubItems[i].Text;
                }

                result.Add(subItems);
            }

            return result;
        }


        /// <summary>
        /// Draws the <see cref="ListView"/> control in detail view with
        /// nice column headers and optional sort function.
        /// </summary>
        /// <param name="listView">The listview control.</param>
        /// <param name="sortOrder">The initial sort order.</param>
        public static void DrawCustomized(this ListView listView, SortOrder sortOrder)
        {
            PropertyInfo prop = listView.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            prop.SetValue(listView, true, null);

            listView.View = View.Details;
            listView.GridLines = true;
            listView.FullRowSelect = true;
            listView.OwnerDraw = true;
            listView.Sorting = sortOrder;

            listView.DrawColumnHeader += RedrawListViewColumnHeader;
            listView.DrawItem += RedrawListViewItem;

            listView.AdjustColumnsWidth();
            listView.FindForm().ResizeEnd += (sender, e) => AdjustColumnsWidth(listView, e);

            if (listView.Sorting == SortOrder.None) return;

            listView.HeaderStyle = ColumnHeaderStyle.Clickable;

            listView.InitialSortListView();
            listView.ColumnClick += SortListView;
        }


        /// <summary>
        /// Adjusts the column widths of the <see cref="ListView"/> control so that
        /// the total width of all columns fills the control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void AdjustColumnsWidth(object sender, EventArgs e)
        {
            ListView listView = sender as ListView;
            listView.AdjustColumnsWidth();
        }


        /// <summary>
        /// Adjusts the column widths of the <see cref="ListView"/> control so that
        /// the total width of all columns fills the control.
        /// </summary>
        /// <param name="listView"></param>
        public static void AdjustColumnsWidth(this ListView listView)
        {
            float columnsWidth = 0;

            if (listView.Columns.Count == 0 || listView.ClientSize.Width == 0) return;

            foreach (ColumnHeader column in listView.Columns)
            {
                columnsWidth += column.Width;
            }

            float widthFactor = listView.ClientSize.Width / columnsWidth;

            foreach (ColumnHeader column in listView.Columns)
            {
                column.Width = (int)Math.Floor(column.Width * widthFactor);
            }
        }


        /// <summary>
        /// Draws the column header of the <see cref="ListView"/> control with
        /// SystemColor.Control colored Background and SystemColor.ControlDark
        /// colored border.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void RedrawListViewColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.Graphics.FillRectangle(SystemBrushes.Control, e.Bounds);

            e.Graphics.DrawLine(
                SystemPens.ControlDark,
                new Point(e.Bounds.X, e.Bounds.Height - 1),
                new Point(e.Bounds.X + e.Bounds.Width, e.Bounds.Height - 1)
            );
            
            if (e.Bounds.X != 0)
            {
                e.Graphics.DrawLine(
                    SystemPens.ControlDark,
                    new Point(e.Bounds.X, 0),
                    new Point(e.Bounds.X, e.Bounds.Height - 1)
                );
            }

            TextRenderer.DrawText(
                e.Graphics,
                e.Header.Text,
                e.Font,
                new Point(e.Bounds.X + 2, (e.Bounds.Height - e.Font.Height) / 2),
                e.ForeColor
            );
        }


        /// <summary>
        /// Draws a <see cref="ListView"/> item with default settings.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void RedrawListViewItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true;

            ListView listView = sender as ListView;
            listView.AdjustColumnsWidth();
        }


        /// <summary>
        /// Sorts the items of a <see cref="ListView"/> by clicking on a
        /// column header. The current sorting is indicated by symbols
        /// in front of the column header.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void SortListView(object sender, ColumnClickEventArgs e)
        {
            ListView listView = sender as ListView;
            int lastSortedColumn = 0;

            foreach (ColumnHeader column in listView.Columns)
            {
                if (column.Text.StartsWith(SortAscSymbol))
                {
                    column.Text = column.Text.Substring(SortAscSymbol.Length);
                    lastSortedColumn = column.Index;
                    break;
                }
                if (column.Text.StartsWith(SortDescSymbol))
                {
                    column.Text = column.Text.Substring(SortDescSymbol.Length);
                    lastSortedColumn = column.Index;
                    break;
                }
            }

            if (lastSortedColumn == e.Column)
            {
                switch (listView.Sorting)
                {
                    case SortOrder.Ascending:
                        listView.Sorting = SortOrder.Descending;
                        listView.Columns[e.Column].Text = SortDescSymbol + listView.Columns[e.Column].Text;
                        break;

                    case SortOrder.Descending:
                        listView.Sorting = SortOrder.Ascending;
                        listView.Columns[e.Column].Text = SortAscSymbol + listView.Columns[e.Column].Text;
                        break;
                }
            }
            else
            {
                listView.Sorting = SortOrder.Ascending;
                listView.Columns[e.Column].Text = SortAscSymbol + listView.Columns[e.Column].Text;
            }

            listView.ListViewItemSorter = new ListViewItemComparer(e.Column, listView.Sorting);
            listView.Sort();
        }


        /// <summary>
        /// The current sorting is indicated by symbols in front of the
        /// column header.
        /// </summary>
        /// <param name="listView"></param>
        private static void InitialSortListView(this ListView listView)
        {
            if (listView.Columns.Count == 0) return;

            switch (listView.Sorting)
            {
                case SortOrder.Ascending:
                    listView.Columns[0].Text = SortAscSymbol + listView.Columns[0].Text;
                    break;

                case SortOrder.Descending:
                    listView.Columns[0].Text = SortDescSymbol + listView.Columns[0].Text;
                    break;
            }
        }


        /// <summary>
        /// Comparer class with methods to sort a listview.
        /// </summary>
        private class ListViewItemComparer : IComparer
        {
            private readonly int _column;
            private readonly SortOrder _sortOrder;

            /// <summary>
            /// Constructor without parameters.
            /// </summary>
            public ListViewItemComparer()
            {
                _column = 0;
                _sortOrder = SortOrder.Ascending;
            }

            /// <summary>
            /// Constructor with parameters index of the column to sort and sort order.
            /// </summary>
            /// <param name="column">column index</param>
            /// <param name="sortOrder">sort order</param>
            public ListViewItemComparer(int column, SortOrder sortOrder)
            {
                _column = column;
                _sortOrder = sortOrder;
            }

            /// <summary>
            /// Compare function.
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns></returns>
            public int Compare(object x, object y)
            {
                int result;

                try
                {
                    result = string.Compare(
                        ((ListViewItem)x).SubItems[_column].Text,
                        ((ListViewItem)y).SubItems[_column].Text
                    );
                }
                catch 
                {
                    return 0;
                }

                if (_sortOrder == SortOrder.Descending)
                {
                    result *= -1;
                }

                return result;
            }
        }
    }
}
