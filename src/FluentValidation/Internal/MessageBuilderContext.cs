namespace FluentValidation.Internal {
	using System;
	using Resources;
	using Validators;

	public class MessageBuilderContext {
		private IPropertyValidatorContext<object,object> _innerContext;

		public MessageBuilderContext(IPropertyValidatorContext<object,object> innerContext, IPropertyValidator<object,object> propertyValidator) {
			_innerContext = innerContext;
			PropertyValidator = propertyValidator;
		}

		public IPropertyValidator<object,object> PropertyValidator { get; }

		public IValidationContext ParentContext => (IValidationContext) _innerContext.ParentContext;

		public PropertyRule Rule => _innerContext.Rule;

		public string PropertyName => _innerContext.PropertyName;

		public string DisplayName => _innerContext.DisplayName;

		public MessageFormatter MessageFormatter => _innerContext.MessageFormatter;

		public object InstanceToValidate => _innerContext.InstanceToValidate;
		public object PropertyValue => _innerContext.PropertyValue;

		public string GetDefaultMessage() {
			return PropertyValidator.GetErrorMessage(_innerContext);
		}
	}
}
