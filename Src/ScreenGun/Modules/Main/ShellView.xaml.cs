﻿// ScreenGun
// - ShellView.xaml.cs
// --------------------------------------------------------------------
// Authors: 
// - Jeff Hansen <jeff@jeffijoe.com>
// - Bjarke Søgaard <ekrajb123@gmail.com>
// Copyright (C) ScreenGun Authors 2015. All rights reserved.

using MahApps.Metro.Controls;

namespace ScreenGun.Modules.Main
{
    /// <summary>
    ///     Shell view.
    /// </summary>
    public partial class ShellView : MetroWindow
    {
        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ShellView" /> class.
        /// </summary>
        public ShellView()
        {
            this.InitializeComponent();
            this.Loaded += (sender, e) =>
            {
                this.ViewModel.ToggleWindow = (show) =>
                {
                    this.WindowState = show ? System.Windows.WindowState.Normal : System.Windows.WindowState.Minimized;
                };
            };
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the view model.
        /// </summary>
        public ShellViewModel ViewModel
        {
            get
            {
                return this.DataContext as ShellViewModel;
            }
        }

        #endregion
    }
}