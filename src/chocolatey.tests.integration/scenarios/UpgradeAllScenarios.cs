// Copyright © 2011 - Present RealDimensions Software, LLC
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

namespace chocolatey.tests.integration.scenarios
{
    using System.Collections.Concurrent;
    using System.Linq;
    using NuGet;
    using Should;
    using bdddoc.core;
    using chocolatey.infrastructure.app.commands;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.results;

    public class UpgradeAllScenarios
    {
        public abstract class ScenariosBase : TinySpec
        {
            protected ConcurrentDictionary<string, PackageResult> Results;
            protected ChocolateyConfiguration Configuration;
            protected IChocolateyPackageService Service;

            public override void Context()
            {
                Configuration = Scenario.upgrade();
                Scenario.reset(Configuration);
                Configuration.PackageNames = Configuration.Input = "all";
                Scenario.add_packages_to_source_location(Configuration, "upgradepackage*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "installpackage*" + Constants.PackageExtension);
                Scenario.install_package(Configuration, "installpackage", "1.0.0");
                Scenario.install_package(Configuration, "upgradepackage", "1.0.0");
                Configuration.SkipPackageInstallProvider = true;

                Service = NUnitSetup.Container.GetInstance<IChocolateyPackageService>();
            }
        }

        [Concern(typeof (ChocolateyUpgradeCommand))]
        public class when_upgrading_all_packages_happy_path : ScenariosBase
        {
            public override void Because()
            {
                Results = Service.upgrade_run(Configuration);
            }

            [Fact]
            public void should_report_for_all_installed_packages()
            {
                Results.Count().ShouldEqual(2);
            }

            [Fact]
            public void should_upgrade_packages_with_upgrades()
            {
                var upgradePackageResult = Results.Where(x => x.Key == "upgradepackage").ToList();
                upgradePackageResult.Count.ShouldEqual(1, "upgradepackage must be there once");
                upgradePackageResult.First().Value.Version.ShouldEqual("1.1.0");
            }

            [Fact]
            public void should_skip_packages_without_upgrades()
            {
                var installPackageResult = Results.Where(x => x.Key == "installpackage").ToList();
                installPackageResult.Count.ShouldEqual(1, "installpackage must be there once");
                installPackageResult.First().Value.Version.ShouldEqual("1.0.0");
            }
        }
    }
}