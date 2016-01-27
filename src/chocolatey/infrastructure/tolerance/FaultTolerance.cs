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

namespace chocolatey.infrastructure.tolerance
{
    using System;
    using System.Threading;
    using configuration;

    /// <summary>
    /// Provides methods that are able to tolerate faults and recover
    /// </summary>
    public class FaultTolerance
    {
        private static bool log_is_in_debug_mode()
        {
            var debugging = false;
            try
            {
                debugging = Config.get_configuration_settings().Debug;
            }
            catch
            {
                //move on - debugging is false
            }

            return debugging;
        }

        /// <summary>
        /// Tries an action the specified number of tries, warning on each failure and raises error on the last attempt.
        /// </summary>
        /// <param name="numberOfTries">The number of tries.</param>
        /// <param name="action">The action.</param>
        /// <param name="description">A message describing what the <paramref name="action"/> is. Shown when an error occurs.</param>
        /// <param name="waitDurationMilliseconds">The wait duration in milliseconds.</param>
        /// <param name="increaseRetryByMilliseconds">The time for each try to increase the wait duration by in milliseconds.</param>
        public static void retry(int numberOfTries, Action action, string description = null, int waitDurationMilliseconds = 100, int increaseRetryByMilliseconds = 0)
        {
            if (action == null) return;

            var success = retry(
                numberOfTries,
                () =>
                    {
                        action.Invoke();
                        return true;
                    },
                description,
                waitDurationMilliseconds,
                increaseRetryByMilliseconds);
        }

        /// <summary>
        /// Tries a function the specified number of tries, warning on each failure and raises error on the last attempt.
        /// </summary>
        /// <typeparam name="T">The type of the return value from the function.</typeparam>
        /// <param name="numberOfTries">The number of tries.</param>
        /// <param name="function">The function.</param>
        /// <param name="description">A message describing what the <paramref name="function"/> is. Shown when an error occurs.</param>
        /// <param name="waitDurationMilliseconds">The wait duration in milliseconds.</param>
        /// <param name="increaseRetryByMilliseconds">The time for each try to increase the wait duration by in milliseconds.</param>
        /// <returns>The return value from the function</returns>
        /// <exception cref="System.ApplicationException">You must specify a number of retries greater than zero.</exception>
        public static T retry<T>(int numberOfTries, Func<T> function, string description = null, int waitDurationMilliseconds = 100, int increaseRetryByMilliseconds = 0)
        {
            if (function == null) return default(T);
            if (numberOfTries == 0) throw new ApplicationException("You must specify a number of retries greater than zero.");
            var returnValue = default(T);

            var debugging = log_is_in_debug_mode();

            for (int i = 1; i <= numberOfTries; i++)
            {
                try
                {
                    returnValue = function.Invoke();
                    break;
                }
                catch (Exception ex)
                {
                    var exceptionMessage = debugging ? ex.ToString() : ex.Message;

                    var failedMessage = "Operation failed ({0}/{1}): {2}".format_with(i, numberOfTries, exceptionMessage);
                    "chocolatey".Log().Warn(failedMessage);
                    
                    if (i == numberOfTries)
                    {
                        "chocolatey".Log().Error(description != null
                            ? "Chocolatey could not execute: {0}".format_with(description)
                            : "Chocolatey could not execute the operation.");

                        throw;
                    }

                    int retryWait = waitDurationMilliseconds + (i * increaseRetryByMilliseconds);

                    "chocolatey".Log().Info("Retrying after {0} milliseconds...".format_with(retryWait));
                    Thread.Sleep(retryWait);
                }
            }

            return returnValue;
        }

        /// <summary>
        /// Wraps an action with a try/catch, logging an error when it fails.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="throwError">if set to <c>true</c> [throw error].</param>
        /// <param name="logWarningInsteadOfError">if set to <c>true</c> log as warning instead of error.</param>
        public static void try_catch_with_logging_exception(Action action, string errorMessage, bool throwError = false, bool logWarningInsteadOfError = false)
        {
            if (action == null) return;

            var success = try_catch_with_logging_exception(
                () =>
                    {
                        action.Invoke();
                        return true;
                    },
                errorMessage,
                throwError,
                logWarningInsteadOfError
                );
        }

        /// <summary>
        /// Wraps a function with a try/catch, logging an error when it fails.
        /// </summary>
        /// <typeparam name="T">The type of the return value from the function.</typeparam>
        /// <param name="function">The function.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="throwError">if set to <c>true</c> [throw error].</param>
        /// <param name="logWarningInsteadOfError">if set to <c>true</c> log as warning instead of error.</param>
        /// <returns>The return value from the function</returns>
        public static T try_catch_with_logging_exception<T>(Func<T> function, string errorMessage, bool throwError = false, bool logWarningInsteadOfError = false)
        {
            if (function == null) return default(T);
            var returnValue = default(T);

            try
            {
                returnValue = function.Invoke();
            }
            catch (Exception ex)
            {
                var exceptionMessage = log_is_in_debug_mode() ? ex.ToString() : ex.Message;

                if (logWarningInsteadOfError)
                {
                    "chocolatey".Log().Warn("{0}:{1} {2}".format_with(errorMessage, Environment.NewLine, exceptionMessage));
                }
                else
                {
                    "chocolatey".Log().Error("{0}:{1} {2}".format_with(errorMessage, Environment.NewLine, exceptionMessage));
                }

                if (throwError)
                {
                    throw;
                }
            }

            return returnValue;
        }
    }
}