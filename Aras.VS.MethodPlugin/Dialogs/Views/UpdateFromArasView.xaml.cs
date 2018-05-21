﻿//------------------------------------------------------------------------------
// <copyright file="UpdateFromArasView.xaml.cs" company="Aras Corporation">
//     © 2017-2018 Aras Corporation. All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.Windows;

namespace Aras.VS.MethodPlugin.Dialogs.Views
{
	/// <summary>
	/// Interaction logic for OpenFromPackageView.xaml
	/// </summary>
	public partial class UpdateFromArasView : Window
	{
		public UpdateFromArasView()
		{
			InitializeComponent();
		}

		private void CancelClick(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}