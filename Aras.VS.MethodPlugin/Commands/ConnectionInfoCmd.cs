﻿//------------------------------------------------------------------------------
// <copyright file="ConnectionInfoCmd.cs" company="Aras Corporation">
//     © 2017-2018 Aras Corporation. All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using Aras.VS.MethodPlugin.Authentication;
using Aras.VS.MethodPlugin.Dialogs;
using Aras.VS.MethodPlugin.ProjectConfigurations;
using Aras.VS.MethodPlugin.SolutionManagement;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Aras.VS.MethodPlugin.Commands
{
	/// <summary>
	/// Command handler
	/// </summary>
	internal sealed class ConnectionInfoCmd : CmdBase
	{
		/// <summary>
		/// Command ID.
		/// </summary>
		public const int CommandId = 0x104;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = CommandIds.ConnectionInfo;

		private readonly IAuthenticationManager authManager;

		private ConnectionInfoCmd(IProjectManager projectManager, IAuthenticationManager authManager, IDialogFactory dialogFactory, IProjectConfigurationManager projectConfigurationManager)
			: base(projectManager, dialogFactory, projectConfigurationManager)
		{
			if (authManager == null) throw new ArgumentNullException(nameof(authManager));

			this.authManager = authManager;

			if (projectManager.CommandService != null)
			{
				var menuCommandID = new CommandID(CommandSet, CommandId);
				var menuItem = new OleMenuCommand(this.ExecuteCommand, menuCommandID);
				menuItem.BeforeQueryStatus += CheckCommandAccessibility;
				projectManager.CommandService.AddCommand(menuItem);
			}
		}

		/// <summary>
		/// Gets the instance of the command.
		/// </summary>
		public static ConnectionInfoCmd Instance
		{
			get;
			private set;
		}

		/// <summary>
		/// Initializes the singleton instance of the command.
		/// </summary>
		/// <param name="package">Owner package, not null.</param>
		public static void Initialize(IProjectManager projectManager, IAuthenticationManager authManager, IDialogFactory dialogFactory, IProjectConfigurationManager projectConfigurationManager)
		{
			Instance = new ConnectionInfoCmd(projectManager, authManager, dialogFactory, projectConfigurationManager);
		}

		public override void ExecuteCommandImpl(object sender, EventArgs args, IVsUIShell uiShell)
		{
			var view = dialogFactory.GetConnectionInfoView(projectManager, projectConfigurationManager);
			view.ShowDialog();
		}
	}
}
