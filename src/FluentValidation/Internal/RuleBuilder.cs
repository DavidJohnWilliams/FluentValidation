#region License
// Copyright (c) .NET Foundation and contributors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// The latest version of this file can be found at https://github.com/FluentValidation/FluentValidation
#endregion

namespace FluentValidation.Internal {
	using System;
	using System.Collections.Generic;
	using Validators;

#pragma warning disable 618
	internal class RuleBuilder<T, TProperty> : IRuleBuilderOptions<T, TProperty>, IRuleBuilderInitial<T, TProperty>, IRuleBuilderInitialCollection<T,TProperty> {
#pragma warning restore 618
		private readonly PropertyRule _rule;
		private readonly AbstractValidator<T> _parentValidator;

		public RuleBuilder(PropertyRule rule, AbstractValidator<T> parent) {
			_rule = rule;
			_parentValidator = parent;
		}

		public IRuleBuilderOptions<T, TProperty, TValidator> SetValidator<TValidator>(TValidator validator) where TValidator : IPropertyValidator {
			if (validator == null) throw new ArgumentNullException(nameof(validator), "Cannot pass a null validator to SetValidator.");
			_rule.AddValidator(validator);
			return new ScopedRuleBuilder<T, TProperty, TValidator>(_rule, _parentValidator, validator);
		}

		IRuleBuilderInitial<T, TProperty> IRuleBuilderInitial<T, TProperty>.Configure(Action<PropertyRule> configurator) {
			configurator(_rule);
			return this;
		}

#pragma warning disable 618
		IRuleBuilderOptions<T, TProperty> IRuleBuilderOptions<T, TProperty>.Configure(Action<PropertyRule> configurator) {
			configurator(_rule);
			return this;
		}
#pragma warning restore 618

		IRuleBuilderInitialCollection<T, TProperty> IRuleBuilderInitialCollection<T, TProperty>.Configure(Action<CollectionPropertyRule<T, TProperty>> configurator) {
			configurator((CollectionPropertyRule<T, TProperty>) _rule);
			return this;
		}

		public IRuleBuilderInitial<T, TNew> Transform<TNew>(Func<TProperty, TNew> transformationFunc) {
			if (transformationFunc == null) throw new ArgumentNullException(nameof(transformationFunc));
			_rule.Transformer = transformationFunc.CoerceToNonGeneric();
			return new RuleBuilder<T, TNew>(_rule, _parentValidator);
		}
	}

	internal class ScopedRuleBuilder<T, TProperty, TValidator> : IRuleBuilderOptions<T,TProperty,TValidator> {
		private readonly PropertyRule _rule;
		private readonly AbstractValidator<T> _parentValidator;
		private readonly TValidator _validator;

		public ScopedRuleBuilder(PropertyRule rule, AbstractValidator<T> parentValidator, TValidator validator) {
			_rule = rule;
			_parentValidator = parentValidator;
			_validator = validator;
		}

		public IRuleBuilderOptions<T, TProperty, TNewValidator> SetValidator<TNewValidator>(TNewValidator validator) where TNewValidator : IPropertyValidator {
			if (validator == null) throw new ArgumentNullException(nameof(validator), "Cannot pass a null validator to SetValidator.");
			_rule.AddValidator(validator);
			return new ScopedRuleBuilder<T, TProperty, TNewValidator>(_rule, _parentValidator, validator);
		}

		public IRuleBuilderOptions<T, TProperty, TValidator> Configure(Action<PropertyRule, TValidator> configurator) {
			configurator(_rule, _validator);
			return this;
		}

		public IRuleBuilderOptions<T, TProperty, TValidator> DependentRules(Action action) {
			var dependencyContainer = new List<IValidationRule>();

			// Capture any rules added to the parent validator inside this delegate.
			using (_parentValidator.Rules.Capture(dependencyContainer.Add)) {
				action();
			}

			if (_rule.RuleSets.Length > 0) {
				foreach (var dependentRule in dependencyContainer) {
					if (dependentRule is PropertyRule propRule && propRule.RuleSets.Length == 0) {
						propRule.RuleSets = _rule.RuleSets;
					}
				}
			}

			_rule.DependentRules.AddRange(dependencyContainer);
			return this;
		}

#pragma warning disable 618
		public IRuleBuilderOptions<T, TProperty> Configure(Action<PropertyRule> configurator) {
			configurator(_rule);
			return this;
		}
#pragma warning restore 618
	}
}
