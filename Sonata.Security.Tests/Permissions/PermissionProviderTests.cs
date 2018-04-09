﻿using Sonata.Security.Extensions;
using Sonata.Security.Permissions;
using System;
using System.Linq;
using Xunit;

namespace Sonata.Security.Tests.Permissions
{
	public class PermissionProviderTests
	{
		[Fact]
		public void PrologStructCanBeSerializedAsString()
		{
			var arguments = new[] { "argument1", "argument2", null }
				.Select(arg => arg.AsTerm())
				.ToArray();

			var goal = $"authorisation({String.Join(", ", arguments)}).";

			Assert.Equal("authorisation(argument1, argument2, _).", goal);
		}

		public class PermissionProviderTestBench : IDisposable
		{
			protected readonly string FactsFilePath;
			protected readonly string RulesFilePath;
			protected readonly PermissionProvider Provider;

			public PermissionProviderTestBench()
			{
				FactsFilePath = System.IO.Path.GetTempFileName();
				RulesFilePath = System.IO.Path.GetTempFileName();
				Provider = new PermissionProvider(FactsFilePath, RulesFilePath);
			}

			private void ReleaseUnmanagedResources()
			{
				if (FactsFilePath != null)
				{
					System.IO.File.Delete(FactsFilePath);
				}
				if (RulesFilePath != null)
				{
					System.IO.File.Delete(RulesFilePath);
				}
			}

			public void Dispose()
			{
				ReleaseUnmanagedResources();
				GC.SuppressFinalize(this);
			}

			~PermissionProviderTestBench()
			{
				ReleaseUnmanagedResources();
			}
		}

		public class FactsTests : PermissionProviderTestBench
		{
			[Fact]
			public void AddFactAddsANewFactToTheFile()
			{
				Provider.Fetch();

				const string fact = "admin(xyz).";
				Provider.AddFact(fact);

				var lastFact = System.IO.File.ReadAllLines(FactsFilePath).LastOrDefault();
				Assert.Equal(fact, lastFact);
				Assert.True(Provider.Eval("admin", "xyz"));
			}

			[Fact]
			public void DuplicateAFactDoesNotChangeTheFile()
			{
				Provider.Fetch();

				const string fact = "admin(xyz).";
				Provider.AddFact(fact);
				Provider.AddFact(fact);

				var facts = System.IO.File.ReadAllLines(FactsFilePath);
				var duplicates = facts
					.GroupBy(f => f)
					.Where(factsGroup => factsGroup.Count() > 1)
					.Select(factsGroup => factsGroup.Key);

				Assert.Empty(duplicates);
				Assert.Contains(fact, facts);
				Assert.True(Provider.Eval("admin", "xyz"));
			}

			[Fact]
			public void RemoveFactRemovesTheFact()
			{
				var initialContent = new[] { "admin(abc).", "admin(def).", "admin(xyz)." };
				System.IO.File.WriteAllLines(FactsFilePath, initialContent);

				Provider.Fetch();

				var factToRemove = "admin(def).";

				Provider.RemoveFact(factToRemove);

				var facts = System.IO.File.ReadAllLines(FactsFilePath);

				Assert.Equal(2, facts.Length);
				Assert.DoesNotContain(factToRemove, facts);
				Assert.False(Provider.Eval("admin", "def"));
			}
		}

		public class RuntimeTests : PermissionProviderTestBench
		{
			[Fact]
			public void PrologEngineIsCreatedAfterLoad()
			{
				Provider.Fetch();

				Assert.NotNull(Provider.PrologEngine);
			}

			[Fact]
			public void PrologFactsAreLoadedInitially()
			{
				var initialContent = new[] { "answerToLifeTheUniverseAndEverything(42).", "collab(afi).", "collab(lma)." };
				System.IO.File.WriteAllLines(FactsFilePath, initialContent);

				Provider.Fetch();

				Assert.True(Provider.Eval("answerToLifeTheUniverseAndEverything", "42"));
				
				var solveResults = Provider.PrologEngine.GetAllSolutions(null, "collab(Collab)");
				Assert.True(solveResults.Success);
				Assert.Equal(2, solveResults.Count);
				Assert.Equal("afi", solveResults.NextSolution.ElementAt(0).NextVariable.Single(e => e.Name == "Collab").Value);
				Assert.Equal("lma", solveResults.NextSolution.ElementAt(1).NextVariable.Single(e => e.Name == "Collab").Value);
			}
		}

		public class AuthorisationTests : PermissionProviderTestBench
		{
			[Fact]
			public void IsAuthorisedReturnsTrueIfRuleExistsInProlog()
			{
				var ruleset = new[] { $"{PermissionProvider.DefaultRuleName}(User,_,_,_):-isUser(User)." };
				System.IO.File.WriteAllLines(RulesFilePath, ruleset);

				var facts = new[] { "isUser(alice).", "isUser(bob)." };
				System.IO.File.WriteAllLines(FactsFilePath, facts);

				Provider.Fetch();

				var request = new PermissionRequest { User = "bob" };

				Assert.True(Provider.IsAuthorized(request));
			}
		}
	}
}
