namespace App.Validation {
	using System.ComponentModel.DataAnnotations;

	public class ValidationError : Exception {
		public ValidationError() : base() { }
		public ValidationError(string message) : base(message) { }
		public ValidationError(string message, Exception innerException)
			: base(message, innerException) { }
	}

	public static class Validator {
		public static void Validate<T>(T obj) where T : class {
			try { 
				System.ComponentModel.DataAnnotations.Validator.ValidateObject(obj, new ValidationContext(obj));
			} catch (ValidationException e) { throw new ValidationError(e.Message); }
		}

		public static void Validate(object? obj, ValidationAttribute[] attributes) {
			if (obj is null)
				throw new ValidationError("Object is null.");
			
			try {
				System.ComponentModel.DataAnnotations.Validator.ValidateValue(obj, new ValidationContext(obj), attributes);
			} catch (ValidationException e) { throw new ValidationError(e.Message); }
		}
	}
}