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
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using Internal;
	using Results;

	public abstract class PropertyValidator<T, TProperty> :  IPropertyValidator<T,TProperty> {
		private string _errorMessage;
		private Func<IPropertyValidatorContext<T,TProperty>, string> _errorMessageFactory;
		private Func<IValidationContext<T>, bool> _condition;
		private Func<IValidationContext<T>, CancellationToken, Task<bool>> _asyncCondition;

		protected PropertyValidator(string errorMessage) {
			SetErrorMessage(errorMessage);
		}

		protected PropertyValidator() {
		}

		/// <summary>
		/// Whether or not this validator has a condition associated with it.
		/// </summary>
		public bool HasCondition => _condition != null;

		/// <summary>
		/// Whether or not this validator has an async condition associated with it.
		/// </summary>
		public bool HasAsyncCondition => _asyncCondition != null;

		/// <summary>
		/// Function used to retrieve custom state for the validator
		/// </summary>
		public Func<IPropertyValidatorContext<T,TProperty>, object> CustomStateProvider { get; set; }

		/// <summary>
		/// Function used to retrieve the severity for the validator
		/// </summary>
		public Func<IPropertyValidatorContext<T,TProperty>, Severity> SeverityProvider { get; set; }

		/// <summary>
		/// Retrieves the error code.
		/// </summary>
		public string ErrorCode { get; set; }

		/// <summary>
		/// Returns the default error message template for this validator, when not overridden.
		/// </summary>
		/// <returns></returns>
		protected virtual string GetDefaultMessageTemplate() => "No default error message has been specified";

		/// <inheritdoc />
		public string GetErrorMessage(IPropertyValidatorContext<T,TProperty> context) {
			string rawTemplate = _errorMessageFactory?.Invoke(context) ?? _errorMessage ?? GetDefaultMessageTemplate();

			if (context == null) {
				return rawTemplate;
			}

			return context.MessageFormatter.BuildMessage(rawTemplate);
		}

		/// <summary>
		/// Sets the overridden error message template for this validator.
		/// </summary>
		/// <param name="errorFactory">A function for retrieving the error message template.</param>
		public void SetErrorMessage(Func<IPropertyValidatorContext<T,TProperty>, string> errorFactory) {
			_errorMessageFactory = errorFactory;
			_errorMessage = null;
		}

		/// <summary>
		/// Sets the overridden error message template for this validator.
		/// </summary>
		/// <param name="errorMessage">The error message to set</param>
		public void SetErrorMessage(string errorMessage) {
			_errorMessage = errorMessage;
			_errorMessageFactory = null;
		}

		/// <summary>
		/// Retrieves a localized string from the LanguageManager.
		/// If an ErrorCode is defined for this validator, the error code is used as the key.
		/// If no ErrorCode is defined (or the language manager doesn't have a translation for the error code)
		/// then the fallback key is used instead.
		/// </summary>
		/// <param name="fallbackKey">The fallback key to use for translation, if no ErrorCode is available.</param>
		/// <returns>The translated error message template.</returns>
		protected string Localized(string fallbackKey) {
			var errorCode = ErrorCode;

			if (errorCode != null) {
				string result = ValidatorOptions.Global.LanguageManager.GetString(errorCode);

				if (!string.IsNullOrEmpty(result)) {
					return result;
				}
			}

			return ValidatorOptions.Global.LanguageManager.GetString(fallbackKey);
		}

		/// <summary>
		/// Adds a condition for this validator. If there's already a condition, they're combined together with an AND.
		/// </summary>
		/// <param name="condition"></param>
		public void ApplyCondition(Func<IValidationContext<T>, bool> condition) {
			if (_condition == null) {
				_condition = condition;
			}
			else {
				var original = _condition;
				_condition = ctx => condition(ctx) && original(ctx);
			}
		}

		/// <summary>
		/// Adds a condition for this validator. If there's already a condition, they're combined together with an AND.
		/// </summary>
		/// <param name="condition"></param>
		public void ApplyAsyncCondition(Func<IValidationContext<T>, CancellationToken, Task<bool>> condition) {
			if (_asyncCondition == null) {
				_asyncCondition = condition;
			}
			else {
				var original = _asyncCondition;
				_asyncCondition = async (ctx, ct) => await condition(ctx, ct) && await original(ctx, ct);
			}
		}

		internal bool InvokeCondition(IValidationContext<T> context) {
			if (_condition != null) {
				return _condition(context);
			}

			return true;
		}

		internal async Task<bool> InvokeAsyncCondition(IValidationContext<T> context, CancellationToken token) {
			if (_asyncCondition != null) {
				return await _asyncCondition(context, token);
			}

			return true;
		}

		/// <inheritdoc />
		public virtual bool ShouldValidateAsynchronously(IValidationContext context) {
			// If the user has applied an async condition, then always go through the async path
			// even if validator is being run synchronously.
			if (HasAsyncCondition) return true;
			return false;
		}

			/// <summary>
		/// Prepares the <see cref="MessageFormatter"/> of <paramref name="context"/> for an upcoming <see cref="ValidationFailure"/>.
		/// </summary>
		/// <param name="context">The validator context</param>
		protected virtual void PrepareMessageFormatterForValidationError(IPropertyValidatorContext<T,TProperty> context) {
			context.MessageFormatter.AppendPropertyName(context.DisplayName);
			context.MessageFormatter.AppendPropertyValue(context.PropertyValue);

			// If there's a collection index cached in the root context data then add it
			// to the message formatter. This happens when a child validator is executed
			// as part of a call to RuleForEach. Usually parameters are not flowed through to
			// child validators, but we make an exception for collection indices.
			if (context.ParentContext.RootContextData.TryGetValue("__FV_CollectionIndex", out var index)) {
				// If our property validator has explicitly added a placeholder for the collection index
				// don't overwrite it with the cached version.
				if (!context.MessageFormatter.PlaceholderValues.ContainsKey("CollectionIndex")) {
					context.MessageFormatter.AppendArgument("CollectionIndex", index);
				}
			}
		}

		/// <summary>
		/// Creates an error validation result for this validator.
		/// </summary>
		/// <param name="context">The validator context</param>
		/// <returns>Returns an error validation result.</returns>
		protected virtual ValidationFailure CreateValidationError(IPropertyValidatorContext<T,TProperty> context) {
			var messageBuilderContext = new MessageBuilderContext((IPropertyValidatorContext<object, object>) context, this);

			var error = context.Rule.MessageBuilder != null
				? context.Rule.MessageBuilder(messageBuilderContext)
				: messageBuilderContext.GetDefaultMessage();

			var failure = new ValidationFailure(context.PropertyName, error, context.PropertyValue);
			failure.FormattedMessagePlaceholderValues = context.MessageFormatter.PlaceholderValues;
			failure.ErrorCode = ErrorCode ?? ValidatorOptions.Global.ErrorCodeResolver(this);

			if (CustomStateProvider != null) {
				failure.CustomState = CustomStateProvider(context);
			}

			if (SeverityProvider != null) {
				failure.Severity = SeverityProvider(context);
			}

			return failure;
		}

		protected abstract bool IsValid(IPropertyValidatorContext<T,TProperty> context);

#pragma warning disable 1998
		protected virtual async Task<bool> IsValidAsync(IPropertyValidatorContext<T,TProperty> context, CancellationToken cancellation) {
			return IsValid(context);
		}
#pragma warning restore 1998

		/// <inheritdoc />
		public virtual IEnumerable<ValidationFailure> Validate(IPropertyValidatorContext<T, TProperty> context) {
			if (IsValid(context)) return Enumerable.Empty<ValidationFailure>();

			PrepareMessageFormatterForValidationError(context);
			return new[] { CreateValidationError(context) };
		}

		/// <inheritdoc />
		public virtual async Task<IEnumerable<ValidationFailure>> ValidateAsync(IPropertyValidatorContext<T, TProperty> context, CancellationToken cancellation) {
			if (await IsValidAsync(context, cancellation)) return Enumerable.Empty<ValidationFailure>();

			PrepareMessageFormatterForValidationError(context);
			return new[] {CreateValidationError(context)};
		}
	}

	public abstract class PropertyValidator : PropertyValidator<object,object>, IPropertyValidator {

		[Obsolete("Don't use the Options property; call methods/properties on the validator instance instead. This will be removed in FluentValidation 11.")]
		PropertyValidator IPropertyValidator.Options => this;

		protected PropertyValidator(string errorMessage) {
			SetErrorMessage(errorMessage);
		}

		protected PropertyValidator() {
		}

		public sealed override IEnumerable<ValidationFailure> Validate(IPropertyValidatorContext<object, object> context) {
			// Delegate to the non-generic overloads for backwards compatibility.
			return Validate(PropertyValidatorContext.FromGeneric(context));
		}

		public sealed override Task<IEnumerable<ValidationFailure>> ValidateAsync(IPropertyValidatorContext<object, object> context, CancellationToken cancellation) {
			// Delegate to the non-generic overloads for backwards compatibility.
			return ValidateAsync(PropertyValidatorContext.FromGeneric(context), cancellation);
		}

		protected sealed override bool IsValid(IPropertyValidatorContext<object, object> context) {
			// Delegate to the non-generic overloads for backwards compatibility.
			return IsValid(PropertyValidatorContext.FromGeneric(context));
		}

		protected sealed override Task<bool> IsValidAsync(IPropertyValidatorContext<object, object> context, CancellationToken cancellation) {
			return IsValidAsync(PropertyValidatorContext.FromGeneric(context), cancellation);
		}

		/// <inheritdoc />
		public virtual IEnumerable<ValidationFailure> Validate(PropertyValidatorContext context) {
			if (IsValid(context)) return Enumerable.Empty<ValidationFailure>();

			PrepareMessageFormatterForValidationError(context);
			return new[] { CreateValidationError(context) };
		}

		/// <inheritdoc />
		public virtual async Task<IEnumerable<ValidationFailure>> ValidateAsync(PropertyValidatorContext context, CancellationToken cancellation) {
			if (await IsValidAsync(context, cancellation)) return Enumerable.Empty<ValidationFailure>();

			PrepareMessageFormatterForValidationError(context);
			return new[] {CreateValidationError(context)};
		}

		protected abstract bool IsValid(PropertyValidatorContext context);

#pragma warning disable 1998
		protected virtual async Task<bool> IsValidAsync(PropertyValidatorContext context, CancellationToken cancellation) {
			return IsValid(context);
		}
#pragma warning restore 1998
	}
}
