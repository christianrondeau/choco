﻿// Copyright © 2011 - Present RealDimensions Software, LLC
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// 
// You may obtain a copy of the License at
// 
// 	http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace chocolatey.tests.infrastructure.commandline
{
    using System;
    using System.Collections.Generic;
    using Moq;
    using Should;
    using chocolatey.infrastructure.adapters;
    using chocolatey.infrastructure.commandline;

    public class InteractivePromptSpecs
    {
        public abstract class InteractivePromptSpecsBase : TinySpec
        {
            protected Mock<IConsole> console = new Mock<IConsole>();
            protected IList<string> choices = new List<string>();
            protected string prompt_value;

            public override void Context()
            {
                prompt_value = "hi";

                InteractivePrompt.initialize_with(new Lazy<IConsole>(() => console.Object));

                choices.Add("yes");
                choices.Add("no");
            }

            public void should_have_called_Console_ReadLine()
            {
                console.Verify(c => c.ReadLine(), Times.AtLeastOnce);
            }
        }

        public class when_prompting_with_interactivePrompt_with_errors : InteractivePromptSpecsBase
        {
            private Func<string> prompt;

            public override void Because()
            {
                prompt = () => InteractivePrompt.prompt_for_confirmation(prompt_value, choices);
            }

            [Fact]
            public void should_error_when_the_choicelist_is_null()
            {
                choices = null;
                bool errored = false;
                console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                try
                {
                    prompt();
                }
                catch (Exception)
                {
                    errored = true;
                }

                errored.ShouldBeTrue();
                console.Verify(c => c.ReadLine(), Times.Never);
            }

            [Fact]
            public void should_error_when_the_choicelist_is_empty()
            {
                choices = new List<string>();
                bool errored = false;
                string errorMessage = string.Empty;
                console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                try
                {
                    prompt();
                }
                catch (Exception ex)
                {
                    errored = true;
                    errorMessage = ex.Message;
                }

                errored.ShouldBeTrue();
                errorMessage.ShouldContain("No choices passed in.");
                console.Verify(c => c.ReadLine(), Times.Never);
            }

            [Fact]
            public void should_error_when_the_prompt_input_is_null()
            {
                choices = new List<string> {"bob"};
                prompt_value = null;
                bool errored = false;
                console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                try
                {
                    prompt();
                }
                catch (Exception)
                {
                    errored = true;
                }

                errored.ShouldBeTrue();
                console.Verify(c => c.ReadLine(), Times.Never);
            }
        }

        public class when_prompting_with_interactivePrompt : InteractivePromptSpecsBase
        {
            private Func<string> prompt;

            public override void Because()
            {
                prompt = () => InteractivePrompt.prompt_for_confirmation(prompt_value, choices);
            }

            public override void AfterObservations()
            {
                base.AfterObservations();
                should_have_called_Console_ReadLine();
            }

            [Fact]
            public void should_error_when_no_answer_given()
            {
                bool errored = false;

                console.Setup(c => c.ReadLine()).Returns(""); //Enter pressed
                try
                {
                    prompt();
                }
                catch (Exception)
                {
                    errored = true;
                }
                errored.ShouldBeTrue();
                console.Verify(c => c.ReadLine(), Times.AtLeast(8));
            }

            [Fact]
            public void should_return_first_choice_when_1_is_given()
            {
                console.Setup(c => c.ReadLine()).Returns("1");
                var result = prompt();
                result.ShouldEqual(choices[0]);
            }

            [Fact]
            public void should_return_second_choice_when_2_is_given()
            {
                console.Setup(c => c.ReadLine()).Returns("2");
                var result = prompt();
                result.ShouldEqual(choices[1]);
            }

            [Fact]
            public void should_error_when_any_choice_not_available_is_given()
            {
                bool errored = false;

                console.Setup(c => c.ReadLine()).Returns("3"); //Enter pressed
                try
                {
                    prompt();
                }
                catch (Exception)
                {
                    errored = true;
                }
                errored.ShouldBeTrue();
                console.Verify(c => c.ReadLine(), Times.AtLeast(8));
            }
        }
    }
}