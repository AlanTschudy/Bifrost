﻿#region License
//
// Copyright (c) 2008-2013, Dolittle (http://www.dolittle.com)
//
// Licensed under the MIT License (http://opensource.org/licenses/MIT)
//
// You may not use this file except in compliance with the License.
// You may obtain a copy of the license at 
//
//   http://github.com/dolittle/Bifrost/blob/master/MIT-LICENSE.txt
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using System;
using System.Security.Principal;
using Bifrost.Commands;
using Bifrost.Domain;
using Bifrost.Events;
using Bifrost.Execution;
using Bifrost.Globalization;
using Bifrost.Principal;
using Bifrost.Sagas;
using Bifrost.Security;
using Bifrost.Testing.Exceptions;
using Bifrost.Validation;
using Moq;

namespace Bifrost.Testing
{
    /// <summary>
    /// Wraps up the Bifrost infrastructure for testing the simple processing of a command, including validation, handing and event generation
    /// </summary>
    /// <typeparam name="T">Type of the Command which the Scenario Tests</typeparam>
    public class CommandScenario<T> where T : class, ICommand
    {
        Mock<ICommandValidatorProvider> command_validator_provider;
        ICommandValidationService command_validation_service;
        ICommandCoordinator command_coordinator;
        Mock<ILocalizer> localizer;
        ICommandContextFactory command_context_factory;
        ICommandContextManager command_context_manager;
        Mock<ICommandHandlerManager> command_handler_manager;
        Mock<IUncommittedEventStreamCoordinator> uncommitted_event_stream_coordinator;
        Mock<IEventStore> event_store;
        Mock<ISagaLibrarian> saga_librarian;
        Mock<IProcessMethodInvoker> process_method_invoker;
        Mock<ICommandSecurityManager> command_security_manager_mock;
        Mock<IExecutionContextFactory> execution_context_factory_mock;
        Mock<ICallContext> call_context_mock;
        IExecutionContextManager execution_context_manager;
        ICanValidate<T> null_validator = new NullCommandInputValidator();

        dynamic command_handler;
        ICanValidate<T> input_validator;
        ICanValidate<T> business_validator;
        IPrincipal principal;

        CommandResult result_of_scenario;

        public CommandScenario()
        {
            principal = new GenericPrincipal(new GenericIdentity("test"), new string[] { });
            GeneratedEvents = new UncommittedEventStream(Guid.Empty);
            uncommitted_event_stream_coordinator = new Mock<IUncommittedEventStreamCoordinator>();
            event_store = new Mock<IEventStore>();
            saga_librarian = new Mock<ISagaLibrarian>();
            process_method_invoker = new Mock<IProcessMethodInvoker>();
            call_context_mock = new Mock<ICallContext>();
            execution_context_factory_mock = new Mock<IExecutionContextFactory>();
            execution_context_manager = new ExecutionContextManager(execution_context_factory_mock.Object, call_context_mock.Object);
            command_context_factory = new CommandContextFactory(uncommitted_event_stream_coordinator.Object, saga_librarian.Object, process_method_invoker.Object,
                                                                                                                            execution_context_manager, event_store.Object);
            command_context_manager = new CommandContextManager(command_context_factory);

            command_handler_manager = new Mock<ICommandHandlerManager>();
            command_handler_manager.Setup(m => m.Handle(It.IsAny<ICommand>())).Callback((ICommand c) => command_handler.Handle((dynamic)c));

            localizer = new Mock<ILocalizer>();
            
            command_validator_provider = new Mock<ICommandValidatorProvider>();
            command_validation_service = new CommandValidationService(command_validator_provider.Object);

            command_security_manager_mock = new Mock<ICommandSecurityManager>();
            //TODO: Allow spec'ing of Security
            command_security_manager_mock.Setup(s => s.Authorize(It.IsAny<ICommand>())).Returns(new AuthorizationResult());

            command_coordinator = new CommandCoordinator(
                                        command_handler_manager.Object, 
                                        command_context_manager, 
                                        command_security_manager_mock.Object,
                                        command_validation_service,
                                        localizer.Object);

            input_validator = null_validator;
            business_validator = null_validator;

            uncommitted_event_stream_coordinator.Setup(es => es.Commit(It.IsAny<UncommittedEventStream>()))
                .Callback((UncommittedEventStream ues) => RecordGeneratedEvents(ues));
        }

        /// <summary>
        /// Specifies validators to be used for input and business validation of the <see cref="ICommand">Command</see> in this scenario.
        /// </summary>
        /// <param name="inputValidator">Input Validator to use for input validation</param>
        /// <param name="businessValidator">Business Validator to use for business validation</param>
        public void ValidatedWith(ICanValidate<T> inputValidator, ICanValidate<T> businessValidator)
        {
            input_validator = inputValidator;
            business_validator = businessValidator;
        }

        /// <summary>
        /// Specifies <see cref="ICanValidate">Validator</see>"/> to be used for Input Validation of the <see cref="ICommand">Command</see> in this scenario.
        /// </summary>
        /// <param name="inputValidator">Input validator to be used.</param>
        public void InputValidatedWith(ICanValidate<T> inputValidator)
        {
            ValidatedWith(inputValidator, null_validator);
        }

        /// <summary>
        /// Specifices <see cref="ICanValidate">Validator</see> to be used for Business Validation of the <see cref="ICommand">Command</see> in this scenario.
        /// </summary>
        /// <param name="businessValidator">Business validator to be used.</param>
        public void BusinessRulesValidatedWith(ICanValidate<T> businessValidator)
        {
            ValidatedWith(null_validator, businessValidator);
        }

        /// <summary>
        /// Specifies the <see cref="IHandleCommands"/>instance to be used for handling of the command in this scenario.
        /// </summary>
        /// <param name="commandHandler">Command Handler to be used.</param>
        public void HandledBy(IHandleCommands commandHandler)
        {
            command_handler = commandHandler;
        }

        /// <summary>
        /// Specifies the IPrincipal instance to be used for handling of the command in this scenario.
        /// </summary>
        /// <param name="thisPrincipal">IPrincipal to be used</param>
        public void AsPrincipal(IPrincipal thisPrincipal)
        {
            principal = thisPrincipal;
        }

        /// <summary>
        /// Initiates the scenario by handling a concrete instance of the <see cref="ICommand"/>
        /// </summary>
        /// <param name="command">Concrete instance of the command to be handled</param>
        /// <returns></returns>
        public CommandResult IsHandled(ICommand command)
        {
            if (command_handler == null)
                throw new Exception("You must specify a command handler before calling CommandIsHandled");

            command_validator_provider.Setup(p => p.GetInputValidatorFor(command)).Returns(input_validator);
            command_validator_provider.Setup(p => p.GetBusinessValidatorFor(command)).Returns(business_validator);

            using(var currentPrincipal = CurrentPrincipal.SetPrincipalTo(principal))
            {
                result_of_scenario = command_coordinator.Handle(command);
                return result_of_scenario;
            }
        }

        void RecordGeneratedEvents(UncommittedEventStream ues)
        {
            GeneratedEvents = ues;
        }

        public TE RegisterAggregateRoot<TE>(TE entityToTrack) where TE : AggregateRoot
        {
            command_context_manager.GetCurrent().RegisterForTracking(entityToTrack);
            return entityToTrack;
        }

        /// <summary>
        /// Exposes an <see cref="UncommittedEventStream"/> for Events that are generated by the Scenario
        /// </summary>
        public UncommittedEventStream GeneratedEvents { get; private set; }

        /// <summary>
        /// Indicates if any Events were generated by the scenario
        /// </summary>
        public bool HasGeneratedEvents { get { return GeneratedEvents.HasEvents; } }
        /// <summary>
        /// Indicates if no Events were generated by the scenario
        /// </summary>
        public bool HasNoGeneratedEvents { get { return !GeneratedEvents.HasEvents; } }

        public bool IsSuccessful()
        {
            EnsureScenarioHasBeenRun();
            return result_of_scenario.Success;
        }

        public bool IsUnsuccessful()
        {
            EnsureScenarioHasBeenRun();
            return !result_of_scenario.Success;
        }

        public void EnsureScenarioHasBeenRun()
        {
            if (result_of_scenario == null)
                throw new CommandScenarioNotRunException();
        }
    }
}