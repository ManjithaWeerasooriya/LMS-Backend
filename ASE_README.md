# 📘 ASE Testing Contributions – LMS Backend

This document summarizes the testing contributions of each team member for the LMS backend project.

---

## 👨‍💻 Team Member 1  
**Name:** A.G.D Hansaja  
**IT Number:** IT23751064  

### 🔍 Focus Area  
- Parameterized Tests  
- Multiple Input Validation  

### ✅ Features Covered  

Three backend features were selected and tested using parameterized inputs:

- **Role Normalization (Authentication)**
  - Validates different role inputs (valid, invalid, whitespace, null)

- **Quiz Answer Validation (DTO)**
  - Tests combinations of selected options, text answers, and file submissions

- **Course Enrollment (Service)**
  - Validates different enrollment outcomes:
    - Course not found  
    - Inactive course  
    - Course full  
    - Already enrolled  
    - Successful enrollment  

### 📊 Test Coverage  

Although only **3 test methods** were written, they collectively cover **18 test cases** using parameterized inputs.

This approach:
- Improves test coverage  
- Reduces code duplication  
- Keeps tests concise and maintainable  

### ▶️ How to Run (Parameterized Tests Only)

```bash
dotnet test LMS-Backend.Tests/LMS-Backend.Tests.csproj --filter FullyQualifiedName~ParameterizedXUnitTests
```

## 👨‍💻 Team Member 2

**Name:** W A A M N Weerasooriya  
**IT Number:** IT23613690 

### 🔍 Focus Area

* Mocking (Service-level using Moq)
* Edge Case Handling (null, invalid ID, exceptions)

### ✅ Contributions

* **Global Exception Handling**

  * Implemented centralized exception handling to standardize API error responses
  * Mapped common exceptions to proper HTTP status codes (400, 401, 403, 404, 409, 500)

* **Service Mocking (Course Module)**

  * Introduced `ICourseService` abstraction
  * Refactored controllers to use dependency injection
  * Implemented Moq-based unit tests for:

    * Unauthorized access
    * Invalid roles
    * Course not found scenarios

* **Material Upload Reliability Improvements**

  * Refactored upload flow to ensure consistency between Azure Blob Storage and database
  * Implemented cleanup logic to remove uploaded files if database operations fail
  * Strengthened edge case handling for:

    * Invalid file types
    * Oversized uploads
    * Unauthorized access
    * Failure scenarios

### 📊 Impact

* Improved backend robustness and fault tolerance
* Increased test reliability through proper mocking
* Enhanced coverage for edge cases and failure scenarios
* Reduced risk of inconsistent data (file upload failures)