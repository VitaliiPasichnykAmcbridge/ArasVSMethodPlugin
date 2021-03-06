﻿using Aras.VS.MethodPlugin.SolutionManagement;
using Aras.VS.MethodPlugin.Dialogs;
using Aras.VS.MethodPlugin.ProjectConfigurations;
using NUnit.Framework;
using Aras.VS.MethodPlugin.Commands;
using NSubstitute;
using Aras.VS.MethodPlugin.Authentication;
using Microsoft.VisualStudio.Shell.Interop;
using System.Threading;
using Aras.VS.MethodPlugin.Code;
using EnvDTE;
using System.IO;
using System;
using Aras.VS.MethodPlugin.Dialogs.Views;
using Aras.VS.MethodPlugin.Tests.Stubs;

namespace Aras.VS.MethodPlugin.Tests.Commands
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class DebugMethodCmdTest
    {
        IAuthenticationManager authManager;
        IProjectManager projectManager;
        IDialogFactory dialogFactory;
        ProjectConfigurationManager projectConfigurationManager;
        DebugMethodCmd debugMethodCmd;
        IVsUIShell iVsUIShell;
        ICodeProviderFactory codeProviderFactory;
        ICodeProvider codeProvider;
        IProjectConfiguraiton projectConfiguration;

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
            iVsUIShell = Substitute.For<IVsUIShell>();
            var currentPath = AppDomain.CurrentDomain.BaseDirectory;
            projectManager.ProjectConfigPath.Returns(Path.Combine(currentPath, "TestData\\projectConfig.xml"));
            projectConfiguration = projectConfigurationManager.Load(projectManager.ProjectConfigPath);
            projectManager.MethodName.Returns("TestMethod");
            projectManager.MethodPath.Returns(Path.Combine(currentPath, "TestData\\TestMethod.txt"));
            DebugMethodCmd.Initialize(projectManager, authManager, dialogFactory, projectConfigurationManager, codeProviderFactory);
            debugMethodCmd = DebugMethodCmd.Instance;

            var project = Substitute.For<Project>();
            var properties = Substitute.For<EnvDTE.Properties>();
            var property = Substitute.For<Property>();
            var propertiesForActiveConfigurations = Substitute.For<EnvDTE.Properties>();
            var propertyForActiveConfiguration = Substitute.For<Property>();
            var configurationManager = Substitute.For<ConfigurationManager>();
            var activeConfigurator = Substitute.For<Configuration>();

            projectManager.SelectedProject.Returns(project);
            project.FileName.Returns(currentPath);
            project.Properties.Returns(properties);
            properties.Item(Arg.Any<string>()).Returns(property);
            property.Value = "";

            project.ConfigurationManager.Returns(configurationManager);
            configurationManager.ActiveConfiguration.Returns(activeConfigurator);
            activeConfigurator.Properties.Returns(propertiesForActiveConfigurations);
            propertiesForActiveConfigurations.Item(Arg.Any<string>()).Returns(propertyForActiveConfiguration);
            propertyForActiveConfiguration.Value = "";
            projectManager.When(x => x.AttachToProcess(Arg.Any<System.Diagnostics.Process>())).Do(x => { });
            var codeModel = Substitute.For<CodeModel>();
            project.CodeModel.Returns(codeModel);
            codeModel.Language.Returns("");
        }


        [Test]
        public void ExecuteCommandImpl_ShouldReceivedGetDebugMethodView()
        {
            // Arrange
            dialogFactory.GetDebugMethodView(null, null, null, null, null, null, null, null).ReturnsForAnyArgs(Substitute.For<DebugMethodViewAdapterTest>());

            //Act
            debugMethodCmd.ExecuteCommandImpl(null, null, iVsUIShell);

            // Assert
            dialogFactory.Received().GetDebugMethodView(iVsUIShell, projectConfigurationManager, Arg.Any<ProjectConfiguraiton>(), Arg.Any<MethodInfo>(), Arg.Any<string>(), projectManager.ProjectConfigPath, string.Empty, string.Empty);

        }

        [Test]
        public void ExecuteCommandImpl_ShouldReceivedLoadMethodCode()
        {
            // Arrange
            dialogFactory.GetDebugMethodView(null, null, null, null, null, null, null, null).ReturnsForAnyArgs(Substitute.For<DebugMethodViewAdapterTest>());

            //Act
            debugMethodCmd.ExecuteCommandImpl(null, null, iVsUIShell);

            // Assert
            codeProvider.Received().LoadMethodCode(Arg.Any<string>(), Arg.Any<MethodInfo>(), string.Empty);
        }

        [Test]
        public void ExecuteCommandImpl_ShouldReceivedAttachToProcess()
        {
            // Arrange
            dialogFactory.GetDebugMethodView(null, null, null, null, null, null, null, null).ReturnsForAnyArgs(Substitute.For<DebugMethodViewAdapterTest>());

            //Act
            debugMethodCmd.ExecuteCommandImpl(null, null, iVsUIShell);

            // Assert
            projectManager.Received().AttachToProcess(Arg.Any<System.Diagnostics.Process>());
        }

        
        public class DebugMethodViewAdapterTest : IViewAdaper<DebugMethodView, DebugMethodViewResult>
        {
            public DebugMethodViewResult ShowDialog()
            {
                return new DebugMethodViewResult
                {
                    DialogOperationResult = true,
                    MethodContext = ""
                };
            }
        }
    }
}
