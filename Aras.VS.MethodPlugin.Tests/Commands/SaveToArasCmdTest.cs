﻿using Aras.VS.MethodPlugin.SolutionManagement;
using Aras.VS.MethodPlugin.Dialogs;
using Aras.VS.MethodPlugin.ProjectConfigurations;
using NUnit.Framework;
using Aras.VS.MethodPlugin.Commands;
using NSubstitute;
using Aras.VS.MethodPlugin.Authentication;
using Microsoft.VisualStudio.Shell.Interop;
using Aras.VS.MethodPlugin.Dialogs.Views;
using System.Threading;
using Aras.VS.MethodPlugin.Code;
using Aras.VS.MethodPlugin.PackageManagement;
using System.IO;
using System;
using Aras.VS.MethodPlugin.Templates;
using System.Dynamic;
using Aras.VS.MethodPlugin.Tests.Stubs;
using System.Windows;

namespace Aras.VS.MethodPlugin.Tests.Commands
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class SaveToArasCmdTest
    {
        IAuthenticationManager authManager;
        IProjectManager projectManager;
        IDialogFactory dialogFactory;
        ProjectConfigurationManager projectConfigurationManager;
        SaveToArasCmd saveToArasCmd;
        IVsUIShell iVsUIShell;
        ICodeProviderFactory codeProviderFactory;
        ICodeProvider codeProvider;
        IProjectConfiguraiton projectConfiguration;
        TemplateLoader templateLoader;
        PackageManager packageManager;

        [SetUp]
        public void Init()
        {
            projectManager = Substitute.For<IProjectManager>();
            projectConfigurationManager = Substitute.For<ProjectConfigurationManager>();
            dialogFactory = Substitute.For<IDialogFactory>();
            authManager = new AuthManagerStub();
            codeProviderFactory = Substitute.For<ICodeProviderFactory>();
            codeProvider = Substitute.For<ICodeProvider>();
            codeProviderFactory.GetCodeProvider(null, null).ReturnsForAnyArgs(codeProvider);
            SaveToArasCmd.Initialize(projectManager, authManager, dialogFactory, projectConfigurationManager, codeProviderFactory);
            saveToArasCmd = SaveToArasCmd.Instance;
            iVsUIShell = Substitute.For<IVsUIShell>();
            var currentPath = AppDomain.CurrentDomain.BaseDirectory;
            projectManager.ProjectConfigPath.Returns(Path.Combine(currentPath, "TestData\\projectConfig.xml"));
            projectConfiguration = projectConfigurationManager.Load(projectManager.ProjectConfigPath);
            templateLoader = new Templates.TemplateLoader();
            projectManager.MethodConfigPath.Returns(Path.Combine(currentPath, "TestData\\method-config.xml"));
            templateLoader.Load(projectManager.MethodConfigPath);
            projectManager.MethodPath.Returns(Path.Combine(currentPath, "TestData\\TestMethod.txt"));
            packageManager = Substitute.For<PackageManager>(authManager);
        }

        [Test]
        public void ExecuteCommandImpl_ShouldReceivedLoadMethodCode()
        {
            // Arrange
            dialogFactory.GetSaveToArasView(null, null, null, null, null, null, null, null, null).ReturnsForAnyArgs(Substitute.For<SaveToArasViewAdapterTest>());
            var messageBox = Substitute.For<IMessageBoxWindow>();
            dialogFactory.GetMessageBoxWindow(null).ReturnsForAnyArgs(messageBox);
            messageBox.ShowDialog(null, null, Arg.Any<MessageButtons>(), Arg.Any<MessageIcon>()).ReturnsForAnyArgs(MessageDialogResult.OK);

            //Act
            saveToArasCmd.ExecuteCommandImpl(null, null, iVsUIShell);

            // Assert
            codeProvider.Received().LoadMethodCode(Arg.Any<string>(), Arg.Any<MethodInfo>(), string.Empty);
        }

        [Test]
        public void ExecuteCommandImpl_ShouldReceivedGetSaveToArasView()
        {
            // Arrange
            dialogFactory.GetSaveToArasView(null, null, null, null, null, null, null, null, null).ReturnsForAnyArgs(Substitute.For<SaveToArasViewAdapterTest>());
            var messageBox = Substitute.For<IMessageBoxWindow>();
            dialogFactory.GetMessageBoxWindow(null).ReturnsForAnyArgs(messageBox);
            messageBox.ShowDialog(null, null, Arg.Any<MessageButtons>(), Arg.Any<MessageIcon>()).ReturnsForAnyArgs(MessageDialogResult.OK);

            //Act
            saveToArasCmd.ExecuteCommandImpl(null, null, iVsUIShell);

            // Assert
            dialogFactory.Received().GetSaveToArasView(iVsUIShell, projectConfigurationManager, Arg.Any<ProjectConfiguraiton>(), Arg.Any<PackageManager>(), Arg.Any<MethodInfo>(), string.Empty, projectManager.ProjectConfigPath, string.Empty, string.Empty);
        }
        
        [Test]
        public void ExecuteCommandImpl_ShouldReceivedAddPackageElementToPackageDefinition()
        {
            // Arrange
            dialogFactory.GetSaveToArasView(null, null, null, null, null, null, null, null, null).ReturnsForAnyArgs(Substitute.For<SaveToArasViewAdapterTest>());

            var messageBox = Substitute.For<IMessageBoxWindow>();
            dialogFactory.GetMessageBoxWindow(null).ReturnsForAnyArgs(messageBox);
            messageBox.ShowDialog(null, null, Arg.Any<MessageButtons>(), Arg.Any<MessageIcon>()).ReturnsForAnyArgs(MessageDialogResult.OK);

            //Act
            saveToArasCmd.ExecuteCommandImpl(null, null, iVsUIShell);

            // Assert
            packageManager.Received().AddPackageElementToPackageDefinition(string.Empty, string.Empty, string.Empty);
        }

        public class SaveToArasViewAdapterTest : IViewAdaper<SaveMethodView, SaveMethodViewResult>
        {
            public SaveMethodViewResult ShowDialog()
            {
                return new SaveMethodViewResult
                {
                    DialogOperationResult = true,
                    MethodItem = new MethodItemStub(),
                    CurrentMethodPackage = string.Empty,
                    MethodCode = string.Empty,
                    MethodComment = string.Empty,
                    MethodLanguage = string.Empty,
                    SelectedIdentityId = string.Empty,
                    SelectedIdentityKeyedName = string.Empty,
                    SelectedPackage = string.Empty,
                    TemplateName = string.Empty,
                    MethodName = string.Empty
                };
            }
        }
    }
}
