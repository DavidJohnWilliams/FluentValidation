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

namespace FluentValidation {
	using System;
	using Internal;
	using Validators;

	/// <summary>
	/// Rule builder that starts the chain
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="TProperty"></typeparam>
	public interface IRuleBuilderInitial<T, out TProperty> : IRuleBuilder<T, TProperty> {

		/// <summary>
		/// Transforms the property value before validation occurs.
		/// </summary>
		/// <typeparam name="TNew"></typeparam>
		/// <param name="transformationFunc"></param>
		/// <returns></returns>
		IRuleBuilderInitial<T, TNew> Transform<TNew>(Func<TProperty, TNew> transformationFunc);

		/// <summary>
		/// Configures the rule.
		/// </summary>
		/// <param name="configurator">Action to configure the object.</param>
		/// <returns></returns>
		IRuleBuilderInitial<T, TProperty> Configure(Action<PropertyRule> configurator);
	}

	/// <summary>
	/// Rule builder
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="TProperty"></typeparam>
	public interface IRuleBuilder<T, out TProperty> {
		/// <summary>
		/// Associates a validator with this the property for this rule builder.
		/// </summary>
		/// <param name="validator">The validator to set</param>
		/// <returns></returns>
		IRuleBuilderOptions<T, TProperty, TValidator> SetValidator<TValidator>(TValidator validator)
			where TValidator : IPropertyValidator;
	}


	public interface IRuleBuilderOptionsBase<T, out TProperty, TValidator> : IRuleBuilder<T,TProperty> {
		IRuleBuilderOptions<T, TProperty, TValidator> Configure(Action<PropertyRule, TValidator> configurator);
	}

	/// <summary>
	/// Rule builder
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="TProperty"></typeparam>
	[Obsolete("Please use IRuleBuilderOptions<T,TProperty,TValidator> instead")]
	public interface IRuleBuilderOptions<T, out TProperty> : IRuleBuilderOptionsBase<T, TProperty, IPropertyValidator> {

		/// <summary>
		/// Configures the current object.
		/// </summary>
		/// <param name="configurator">Action to configure the object.</param>
		/// <returns></returns>
		IRuleBuilderOptions<T, TProperty> Configure(Action<PropertyRule> configurator);
	}

	public interface IRuleBuilderOptions<T, out TProperty, TValidator>
#pragma warning disable 618
		: IRuleBuilder<T, TProperty>, IRuleBuilderOptionsBase<T,TProperty, TValidator> {
#pragma warning restore 618
		// This interface needs to inherit from the legacy IRuleBuilderOptions to allow users who still use the old
		// interface to implicitly cast the return value of methods that return this interface. For example, we historically
		// recommended that custom extensions take an IRuleBuilderOptions<T,TProp> and return one, but now the internal
		// methods return this newer interface instead. By implementing the old interface too, we can have an implicit
		// conversion that won't break end users' code. However this means we have to re-declare the Configure method
		// as otherwise the compiler won't know whether to use the one that's implicitly imported via IRuleBuilderOptionsBase
		// or the one that's imported by IRuleBuilderOptions<T,TProperty> (which also inherits from IRuleBuilderOptionsBase).
		// This can all be cleaned up once we remove the legacy interfaces, probably in FV 11.

		/// <summary>
		/// Configures the current object.
		/// </summary>
		/// <param name="configurator">Action to configure the object.</param>
		/// <returns></returns>
		new IRuleBuilderOptions<T, TProperty, TValidator> Configure(Action<PropertyRule, TValidator> configurator);

		[Obsolete("Use the other overload of Configure which takes an Action<PropertyRule, Validator>")]
		IRuleBuilderOptions<T, TProperty, TValidator> Configure(Action<PropertyRule> configurator);

		/// <summary>
		/// Creates a scope for declaring dependent rules.
		/// </summary>
		IRuleBuilderOptions<T, TProperty, TValidator> DependentRules(Action action);
	}

	/// <summary>
	/// Rule builder that starts the chain for a child collection
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="TElement"></typeparam>
	public interface IRuleBuilderInitialCollection<T, TElement> : IRuleBuilder<T, TElement> {

		/// <summary>
		/// Transforms the collection element value before validation occurs.
		/// </summary>
		/// <param name="transformationFunc"></param>
		/// <returns></returns>
		IRuleBuilderInitial<T, TNew> Transform<TNew>(Func<TElement, TNew> transformationFunc);

		/// <summary>
		/// Configures the rule object.
		/// </summary>
		/// <param name="configurator">Action to configure the object.</param>
		/// <returns></returns>
		IRuleBuilderInitialCollection<T, TElement> Configure(Action<CollectionPropertyRule<T, TElement>> configurator);
	}

	/// <summary>
	/// Fluent interface for conditions (When/Unless/WhenAsync/UnlessAsync)
	/// </summary>
	public interface IConditionBuilder {
		/// <summary>
		/// Rules to be invoked if the condition fails.
		/// </summary>
		/// <param name="action"></param>
		void Otherwise(Action action);
	}

}
