﻿using System.Diagnostics;
using System.Web;
using Bifrost.Configuration;
using Bifrost.Execution;
using Bifrost.QuickStart.Concepts.Persons;
using Bifrost.QuickStart.Domain.HumanResources.Employees;
using Bifrost.Validation;

namespace Bifrost.QuickStart
{
    public class BifrostConfigurator : ICanConfigure
    {
        public void Configure(IConfigure configure)
        {
            var dataPath = HttpContext.Current.Server.MapPath("~/App_Data");
            configure
                .Serialization
                    .UsingJson()
                .Events
                    .UsingRavenDB(e=>e.WithUrl("http://localhost:8080").WithDefaultDatabase("QuickStart"))
                    //.UsingRavenDBEmbedded(e=>e.LocatedAt(dataPath).WithManagementStudio())
                .Events
                    .Asynchronous(e=>e.UsingSignalR())
                .DefaultStorage
                    .UsingRavenDB(e => e.WithUrl("http://localhost:8080").WithDefaultDatabase("QuickStart"))
                    //.UsingRavenDBEmbedded(e=>e.LocatedAt(dataPath))
                .Frontend
                    .Web(w=> {
                        w.AsSinglePageApplication();
                        w.PathsToNamespaces.Clear();
                        w.PathsToNamespaces.Add("Features/**/", "Bifrost.QuickStart.Features.**.");
                        w.PathsToNamespaces.Add("/Features/**/", "Bifrost.QuickStart.Features.**.");
                        w.NamespaceMapper.Add("Bifrost.QuickStart.Features.**.", "Bifrost.QuickStart.Domain.HumanResources.**.");
                        w.NamespaceMapper.Add("Bifrost.QuickStart.Features.**.", "Bifrost.QuickStart.Read.HumanResources.**.");
					})
                .WithMimir();

            var validatorProvider = configure.Container.Get<ICommandValidatorProvider>();
            var inputValidator = validatorProvider.GetInputValidatorFor(typeof (RegisterEmployee));
            var businessValidator = validatorProvider.GetBusinessValidatorFor(typeof (RegisterEmployee));

            var command = new RegisterEmployee()
                {
                    SocialSecurityNumber = "invalid"
                };

            var inputResult = inputValidator.ValidateFor(command);
            var businessResult = businessValidator.ValidateFor(command);
        }
    }
}