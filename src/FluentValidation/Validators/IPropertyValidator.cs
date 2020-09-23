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

namespace FluentValidation.Validators {
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;
	using Results;

	public interface IPropertyValidator<T, TProperty> {
		/// <summary>
		/// Whether or not this validator has a condition associated with it.
		/// </summary>
		bool HasCondition { get; }

		/// <summary>
		/// Whether or not this validator has an async condition associated with it.
		/// </summary>
		bool HasAsyncCondition { get; }

		/// <summary>
		/// Function used to retrieve custom state for the validator
		/// </summary>
		Func<IPropertyValidatorContext<T, TProperty>, object> CustomStateProvider { get; set; }

		/// <summary>
		/// Function used to retrieve the severity for the validator
		/// </summary>
		Func<IPropertyValidatorContext<T, TProperty>, Severity> SeverityProvider { get; set; }

		/// <summary>
		/// Retrieves the error code.
		/// </summary>
		string ErrorCode { get; set; }

		/// <summary>
		/// Gets the error message. If no context is specified, the raw message template is returned instead.
		/// </summary>
		/// <param name="context"></param>
		/// <returns>String error message, or template.</returns>
		string GetErrorMessage(IPropertyValidatorContext<T,TProperty> context);

		/// <summary>
		/// Sets the overridden error message template for this validator.
		/// </summary>
		/// <param name="errorFactory">A function for retrieving the error message template.</param>
		void SetErrorMessage(Func<IPropertyValidatorContext<T,TProperty>, string> errorFactory);

		/// <summary>
		/// Sets the overridden error message template for this validator.
		/// </summary>
		/// <param name="errorMessage">The error message to set</param>
		void SetErrorMessage(string errorMessage);

		/// <summary>
		/// Adds a condition for this validator. If there's already a condition, they're combined together with an AND.
		/// </summary>
		/// <param name="condition"></param>
		void ApplyCondition(Func<IValidationContext<T>, bool> condition);

		/// <summary>
		/// Adds a condition for this validator. If there's already a condition, they're combined together with an AND.
		/// </summary>
		/// <param name="condition"></param>
		void ApplyAsyncCondition(Func<IValidationContext<T>, CancellationToken, Task<bool>> condition);

		/// <summary>
		/// Determines whether this validator should be run asynchronously or not.
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		bool ShouldValidateAsynchronously(IValidationContext context);

		/// <summary>
		/// Performs validation
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		IEnumerable<ValidationFailure> Validate(IPropertyValidatorContext<T, TProperty> context);

		/// <summary>
		/// Performs validation asynchronously.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="cancellation"></param>
		/// <returns></returns>
		Task<IEnumerable<ValidationFailure>> ValidateAsync(IPropertyValidatorContext<T, TProperty> context, CancellationToken cancellation);
	}

	/// <summary>
	/// A custom property validator.
	/// This interface should not be implemented directly in your code as it is subject to change.
	/// Please inherit from <see cref="PropertyValidator">PropertyValidator</see> instead.
	/// </summary>
	public interface IPropertyValidator : IPropertyValidator<object, object> {

		/// <summary>
		/// Additional options for configuring the property validator.
		/// </summary>
		[Obsolete("The options property is obsolete. Please call properties/methods on the IPropertyValidator instance directly.")]
		PropertyValidator Options { get; }
	}

}
