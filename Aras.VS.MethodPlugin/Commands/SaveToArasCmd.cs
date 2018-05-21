﻿//------------------------------------------------------------------------------
// <copyright file="SaveToArasCmd.cs" company="Aras Corporation">
//     © 2017-2018 Aras Corporation. All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using Aras.VS.MethodPlugin.Authentication;
using Aras.VS.MethodPlugin.Code;
using Aras.VS.MethodPlugin.Dialogs;
using Aras.VS.MethodPlugin.Dialogs.Views;
using Aras.VS.MethodPlugin.PackageManagement;
using Aras.VS.MethodPlugin.ProjectConfigurations;
using Aras.VS.MethodPlugin.SolutionManagement;
using Aras.VS.MethodPlugin.Templates;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;

namespace Aras.VS.MethodPlugin.Commands
{
	/// <summary>
	/// Command handler
	/// </summary>
	internal sealed class SaveToArasCmd : AuthenticationCommandBase
	{
		/// <summary>
		/// Command ID.
		/// </summary>
		public const int CommandId = 0x101;

		public const int ToolbarCommandId = 0x106;

		/// <summary>
		/// Command menu group (command set GUID).
		/// </summary>
		public static readonly Guid CommandSet = new Guid("694F6136-7CF1-46E1-B9E2-24296488AE96");

		public static readonly Guid ToolbarCommandSet = new Guid("21D122E1-35BF-4156-B458-7E292CDD9C2D");

		private SaveToArasCmd(IProjectManager projectManager, IAuthenticationManager authManager, IDialogFactory dialogFactory, ProjectConfigurationManager projectConfigurationManager, ICodeProviderFactory codeProviderFactory) : base(authManager, dialogFactory, projectManager, projectConfigurationManager, codeProviderFactory)
		{
			if (projectManager.CommandService != null)
			{
				var menuCommandID = new CommandID(CommandSet, CommandId);
				var menuItem = new MenuCommand(this.ExecuteCommand, menuCommandID);
				var toolbarMenuCommandID = new CommandID(ToolbarCommandSet, ToolbarCommandId);
				var toolbarMenuItem = new MenuCommand(this.ExecuteCommand, toolbarMenuCommandID);

				projectManager.CommandService.AddCommand(menuItem);
				projectManager.CommandService.AddCommand(toolbarMenuItem);
			}
		}

		/// <summary>
		/// Gets the instance of the command.
		/// </summary>
		public static SaveToArasCmd Instance
		{
			get;
			private set;
		}

		public static void Initialize(IProjectManager projectManager, IAuthenticationManager authManager, IDialogFactory dialogFactory, ProjectConfigurationManager projectConfigurationManager, ICodeProviderFactory codeProviderFactory)
		{
			Instance = new SaveToArasCmd(projectManager, authManager, dialogFactory, projectConfigurationManager, codeProviderFactory);
		}

		public static string GetUpdateAction(int lockStatus)
		{
			string action;

			switch (lockStatus)
			{
				case 0:
					action = "edit";
					break;
				case 1:
					action = "update";
					break;
				case 2:
					throw new Exception("Item has been locked by someone.");
				default:
					throw new Exception("Get Lock Status Error");
			}

			return action;
		}

		public override void ExecuteCommandImpl(object sender, EventArgs args, IVsUIShell uiShell)
		{
			var project = projectManager.SelectedProject;

			var projectConfigPath = projectManager.ProjectConfigPath;
			var methodConfigPath = projectManager.MethodConfigPath;

			ProjectConfiguraiton projectConfiguration = projectConfigurationManager.Load(projectConfigPath);

			string selectedMethodPath = projectManager.MethodPath;
			string sourceCode = File.ReadAllText(selectedMethodPath);
			string selectedMethodName = Path.GetFileNameWithoutExtension(selectedMethodPath);

			MethodInfo methodInformation = projectConfiguration.MethodInfos.FirstOrDefault(m => m.MethodName == selectedMethodName);
			if (methodInformation == null)
			{
				throw new Exception();
			}

			ICodeProvider codeProvider = codeProviderFactory.GetCodeProvider(project.CodeModel.Language, projectConfiguration);
			string methodCode = codeProvider.LoadMethodCode(sourceCode, methodInformation, projectManager.ServerMethodFolderPath);

			var packageManager = new PackageManager(authManager);
			var saveView = dialogFactory.GetSaveToArasView(uiShell, projectConfigurationManager, projectConfiguration, packageManager, methodInformation, methodCode, projectConfigPath, project.Name, project.FullName);
			var saveViewResult = saveView.ShowDialog();
			if (saveViewResult?.DialogOperationResult != true)
			{
				return;
			}

			var templateLoader = new TemplateLoader();
			templateLoader.Load(methodConfigPath);

			dynamic currentMethodItem = saveViewResult.MethodItem;

			if (!currentMethodItem.isError())
			{
				methodCode = saveViewResult.MethodCode;
				var template = templateLoader.Templates.FirstOrDefault(t => t.TemplateName == saveViewResult.TemplateName);
				if (template != null && !template.IsSupported)
				{
					methodCode = methodCode.Insert(0, string.Format("//MethodTemplateName={0}\r\n", template.TemplateName));
				}
				currentMethodItem.setProperty("method_code", methodCode);
				currentMethodItem.setProperty("name", saveViewResult.MethodName);
				currentMethodItem.setProperty("method_type", saveViewResult.MethodLanguage);
				currentMethodItem.setProperty("execution_allowed_to", saveViewResult.SelectedIdentityId);

				var action = GetUpdateAction(currentMethodItem.getLockStatus());
				currentMethodItem = currentMethodItem.apply(action);
			}
			else
			{
				methodCode = saveViewResult.MethodCode;
				var template = templateLoader.Templates.FirstOrDefault(t => t.TemplateName == saveViewResult.TemplateName);
				if (template != null && !template.IsSupported)
				{
					methodCode = methodCode.Insert(0, string.Format("//MethodTemplateName={0}\r\n", template.TemplateName));
				}

				currentMethodItem = authManager.InnovatorInstance.newItem("Method", "add");
				currentMethodItem.setProperty("method_code", methodCode);
				currentMethodItem.setProperty("name", saveViewResult.MethodName);
				currentMethodItem.setProperty("method_type", saveViewResult.MethodLanguage);
				currentMethodItem.setProperty("execution_allowed_to", saveViewResult.SelectedIdentityId);
				currentMethodItem = currentMethodItem.apply();
			}

			if (currentMethodItem.isError())
			{
				throw new Exception(currentMethodItem.getErrorString());
			}

			var newId = currentMethodItem.getID();
			var currentMethodPackage = packageManager.GetPackageDefinitionByElementName(saveViewResult.MethodName);

			if (string.IsNullOrEmpty(currentMethodPackage))
			{
				packageManager.AddPackageElementToPackageDefinition(newId, saveViewResult.MethodName, saveViewResult.SelectedPackage);
			}
			else
			{
				if (currentMethodPackage != saveViewResult.SelectedPackage)
				{
					var messageWindow = new MessageBoxWindow();
					var dialogReuslt = messageWindow.ShowDialog(null,
						$"The {saveViewResult.MethodName} method already attached to differernt package. Click OK to reasign package for this method.",
						"Save method to Aras Innovator",
						MessageButtons.OKCancel,
						MessageIcon.None);

					if (dialogReuslt == MessageDialogResult.OK)
					{
						packageManager.DeletePackageElementByNameFromPackageDefinition(saveViewResult.MethodName);
						packageManager.AddPackageElementToPackageDefinition(newId, saveViewResult.MethodName, saveViewResult.SelectedPackage);
					}
					else
					{
						saveViewResult.SelectedPackage = currentMethodPackage;
					}
				}
			}

			if (methodInformation.MethodName == saveViewResult.MethodName)
			{
				methodInformation.InnovatorMethodId = newId;
				methodInformation.PackageName = saveViewResult.SelectedPackage;
				methodInformation.ExecutionAllowedToKeyedName = saveViewResult.SelectedIdentityKeyedName;
				methodInformation.ExecutionAllowedToId = saveViewResult.SelectedIdentityId;

				projectConfigurationManager.Save(projectConfigPath, projectConfiguration);
			}

			string message = string.Format("Method \"{0}\" saved", saveViewResult.MethodName);
			var messageBoxWindow = new MessageBoxWindow();
			messageBoxWindow.ShowDialog(null,
				message,
				string.Empty,
				MessageButtons.OK,
				MessageIcon.Information);
		}
	}
}