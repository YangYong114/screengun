﻿// ScreenGun
// - RegionSelectorWindow.xaml.cs
// --------------------------------------------------------------------
// Authors: 
// - Jeff Hansen <jeff@jeffijoe.com>
// - Bjarke Søgaard <ekrajb123@gmail.com>
// Copyright (C) ScreenGun Authors 2015. All rights reserved.

using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace ScreenGun.Modules.RegionSelector
{
    /// <summary>
    ///     Interaction logic for RegionSelectorView.xaml
    /// </summary>
    public partial class RegionSelectorWindow
    {
        #region Fields

        /// <summary>
        ///     The virtual screen.
        /// </summary>
        private readonly Rect virtualScreen = new Rect(
            new Point(
                SystemParameters.VirtualScreenLeft,
                SystemParameters.VirtualScreenTop),
            new Size(
                SystemParameters.VirtualScreenWidth,
                SystemParameters.VirtualScreenHeight));

        /// <summary>
        ///     The end position.
        /// </summary>
        private Point endPosition;

        /// <summary>
        ///     The flag that dictates if we are in full screen.
        /// </summary>
        private bool isFullScreen;

        /// <summary>
        ///     Flag used to determine if the region is being moved.
        /// </summary>
        private bool isMoving;

        /// <summary>
        ///     Flag to determine if the region is being resized.
        /// </summary>
        private bool isResizing;

        /// <summary>
        ///     The mouse position that was determined in the previous event. Used for calculating point deltas.
        /// </summary>
        private Point lastMousePosition;

        /// <summary>
        ///     The recording area.
        /// </summary>
        private Rect relativeRecordingArea;

        /// <summary>
        ///     The start position.
        /// </summary>
        private Point startPosition;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="RegionSelectorWindow" /> class.
        /// </summary>
        public RegionSelectorWindow()
        {
            this.InitializeComponent();
            this.Closed += (sender, args) =>
            {
                this.RegionChange = null;
                this.FullScreenChanged = null;
            };
            this.Top = this.virtualScreen.Top;
            this.Left = this.virtualScreen.Left;
            this.Width = this.virtualScreen.Width;
            this.Height = this.virtualScreen.Height;
            this.SetupInitialRegion();
        }

        #endregion

        #region Public Events

        /// <summary>
        ///     Occurs when full screen changed.
        /// </summary>
        public event FullScreenChanged FullScreenChanged;

        /// <summary>
        ///     Occurs when the region changes.
        /// </summary>
        public event RegionChange RegionChange;

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets a value indicating whether this instance is full screen.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is full screen; otherwise, <c>false</c>.
        /// </value>
        protected bool IsFullScreen
        {
            get
            {
                return this.isFullScreen;
            }

            set
            {
                this.isFullScreen = value;
                if (this.isFullScreen)
                {
                    this.EnableFullScreen();
                }
                else
                {
                    this.UpdatePosition();
                }

                var handler = this.FullScreenChanged;
                if (handler != null)
                {
                    handler.Invoke(new FullScreenChangedArgs(this.isFullScreen));
                }
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether this <see cref="RegionSelectorWindow" /> is locked.
        /// </summary>
        /// <value>
        ///     <c>true</c> if locked; otherwise, <c>false</c>.
        /// </value>
        protected bool Locked { get; set; }

        /// <summary>
        ///     Gets the recording area.
        /// </summary>
        /// <value>
        ///     The recording area.
        /// </value>
        protected Rectangle RecordingArea
        {
            get
            {
                return new Rectangle(
                    (int)(this.relativeRecordingArea.X + this.virtualScreen.X),
                    (int)(this.relativeRecordingArea.Y + this.virtualScreen.Y),
                    (int)this.relativeRecordingArea.Width,
                    (int)this.relativeRecordingArea.Height);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        ///     The enable full screen.
        /// </summary>
        private void EnableFullScreen()
        {
            var position = System.Windows.Forms.Cursor.Position;
            foreach (var screen in Screen.AllScreens)
            {
                var bounds = screen.Bounds;
                if (!bounds.Contains(position))
                {
                    continue;
                }

                var x = bounds.X - this.virtualScreen.X;
                var y = bounds.Y - this.virtualScreen.Y;
                this.relativeRecordingArea = new Rect(
                    x,
                    y,
                    bounds.Width,
                    bounds.Height);
                this.OnRegionChanged();
                break;
            }

            this.UpdateUI();
        }

        /// <summary>
        ///     Called when region changed.
        /// </summary>
        private void OnRegionChanged()
        {
            var handler = this.RegionChange;
            if (handler != null)
            {
                var area = this.RecordingArea;
                handler.Invoke(new RegionChangeArgs(area));
            }
        }

        /// <summary>
        /// Handles the MouseDown event of the OverlayGrid control.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The <see cref="MouseButtonEventArgs"/> instance containing the event data.
        /// </param>
        private void OverlayGridMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (this.isResizing || this.isMoving || this.Locked)
            {
                return;
            }

            this.isResizing = true;

            if (this.IsFullScreen)
            {
                this.IsFullScreen = false;
            }

            this.startPosition = this.endPosition = e.GetPosition((IInputElement)sender);
            this.UpdatePosition();
        }

        /// <summary>
        /// Handles the OnMouseMove event of the OverlayGrid control.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The <see cref="System.Windows.Input.MouseEventArgs"/> instance containing the event data.
        /// </param>
        private void OverlayGridOnMouseMove(object sender, MouseEventArgs e)
        {
            this.TryMoveRegion(e);
            if (this.isResizing == false || this.Locked)
            {
                return;
            }

            this.endPosition = e.GetPosition((IInputElement)sender);
            this.UpdatePosition();
        }

        /// <summary>
        /// Handles the OnMouseUp event of the OverlayGrid control.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The <see cref="MouseButtonEventArgs"/> instance containing the event data.
        /// </param>
        private void OverlayGridOnMouseUp(object sender, MouseButtonEventArgs e)
        {
            this.isResizing = false;
            this.isMoving = false;
            this.UpdatePosition();
        }

        /// <summary>
        /// Handles the MouseLeftDown event of the RecordingRegion control.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The <see cref="MouseButtonEventArgs"/> instance containing the event data.
        /// </param>
        private void RecordingRegionMouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            this.lastMousePosition = e.GetPosition(this.OverlayGrid);
            this.isMoving = true;
        }

        /// <summary>
        /// Handles the MouseLeftUp event of the RecordingRegion control.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The <see cref="MouseButtonEventArgs"/> instance containing the event data.
        /// </param>
        private void RecordingRegionMouseLeftUp(object sender, MouseButtonEventArgs e)
        {
            this.isMoving = false;
            this.isResizing = false;
        }

        /// <summary>
        /// Handles the MouseMove event of the RecordingRegion control.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The <see cref="MouseEventArgs"/> instance containing the event data.
        /// </param>
        private void RecordingRegionMouseMove(object sender, MouseEventArgs e)
        {
            this.TryMoveRegion(e);
        }

        /// <summary>
        /// Handles the MouseDown event of ResizeGrips.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The <see cref="MouseButtonEventArgs"/> instance containing the event data.
        /// </param>
        private void ResizeGripMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (this.isResizing || this.Locked)
            {
                return;
            }

            if (this.IsFullScreen)
            {
                this.IsFullScreen = false;
            }

            Point position = e.GetPosition(this.OverlayGrid);
            this.isResizing = true;
            this.endPosition = position;

            var edges = new[]
            {
                this.relativeRecordingArea.TopLeft, 
                this.relativeRecordingArea.TopRight, 
                this.relativeRecordingArea.BottomLeft, 
                this.relativeRecordingArea.BottomRight
            };

            var furthestAway = new Point();
            double lastTotal = 0;
            foreach (var edge in edges)
            {
                double absX = Math.Abs(edge.X - position.X);
                double absY = Math.Abs(edge.Y - position.Y);
                var total = absX + absY;
                if (total > lastTotal)
                {
                    lastTotal = total;
                    furthestAway = edge;
                }
            }

            this.startPosition = furthestAway;
        }

        /// <summary>
        ///     Setups the initial region.
        /// </summary>
        private void SetupInitialRegion()
        {
            var cursorPos = System.Windows.Forms.Cursor.Position;
            foreach (var screen in Screen.AllScreens)
            {
                if (screen.Bounds.Contains(cursorPos) == false)
                {
                    continue;
                }

                var regionWidth = (double)screen.Bounds.Width / 2;
                var regionHeight = (double)screen.Bounds.Height / 2;
                double x = ((double)screen.Bounds.Width / 2) - (regionWidth / 2);
                double y = ((double)screen.Bounds.Height / 2) - (regionHeight / 2);
                x -= this.virtualScreen.X - screen.Bounds.X;
                y -= this.virtualScreen.Y - screen.Bounds.Y;

                this.startPosition = new Point(x, y);
                this.endPosition = new Point(x + regionWidth, y + regionHeight);
                this.UpdatePosition();
                break;
            }
        }

        /// <summary>
        /// Tries to move the region.
        /// </summary>
        /// <param name="e">
        /// The <see cref="MouseEventArgs"/> instance containing the event data.
        /// </param>
        private void TryMoveRegion(MouseEventArgs e)
        {
            if (this.isMoving == false || this.Locked)
            {
                return;
            }

            var position = e.GetPosition(this.OverlayGrid);
            if (this.lastMousePosition == default(Point))
            {
                this.lastMousePosition = position;
            }

            if (this.IsFullScreen)
            {
                var width = Math.Abs(this.startPosition.X - this.endPosition.X);
                var height = Math.Abs(this.startPosition.Y - this.endPosition.Y);

                this.startPosition = new Point(position.X - (width / 2), position.Y - (height / 2));
                this.endPosition = new Point(position.X + (width / 2), position.Y + (height / 2));

                this.IsFullScreen = false;
            }

            var delta = position - this.lastMousePosition;
            this.lastMousePosition = position;
            this.startPosition += delta;
            this.endPosition += delta;
            this.UpdatePosition();
        }

        /// <summary>
        ///     Updates the position.
        /// </summary>
        private void UpdatePosition()
        {
            var x = Math.Min(this.startPosition.X, this.endPosition.X);
            var y = Math.Min(this.startPosition.Y, this.endPosition.Y);
            var width = Math.Abs(this.startPosition.X - this.endPosition.X);
            var height = Math.Abs(this.startPosition.Y - this.endPosition.Y);
            this.relativeRecordingArea = new Rect(x, y, width, height);

            // This makes sure that the recording area is never out of bounds.
            var relativeVirtualScreen = new Rect(
                this.virtualScreen.X,
                this.virtualScreen.Y,
                this.virtualScreen.Width,
                this.virtualScreen.Height);

            relativeVirtualScreen.Offset(Math.Abs(this.virtualScreen.X), Math.Abs(this.virtualScreen.Y));
            this.relativeRecordingArea.Intersect(relativeVirtualScreen);
            this.UpdateUI();

            this.OnRegionChanged();
        }

        /// <summary>
        ///     Updates the UI.
        /// </summary>
        private void UpdateUI()
        {
            this.LeftColumn.Width = new GridLength(this.relativeRecordingArea.Left);
            this.RightColumn.Width = new GridLength(this.virtualScreen.Width - this.relativeRecordingArea.Right);
            this.TopRow.Height = new GridLength(this.relativeRecordingArea.Top);
            this.BottomRow.Height = new GridLength(this.virtualScreen.Height - this.relativeRecordingArea.Bottom);
        }

        #endregion
    }
}