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

## 👨‍💻 Team Member 3

**Name:** R.M.D.N. Jayaweera  
**IT Number:** IT23742918

### 🔍 Focus Area

* Load Testing (K6)
* Performance & Stress Analysis (ASP.NET Core APIs)

### ✅ Contributions

* **Spike Testing (Login, Materials, Live Sessions)**

  * Simulated sudden user surges (0 → 100 users) on ASP.NET Core API endpoints
  * Evaluated system stability under real-world peak scenarios
  * Detected high failure rate in login endpoint under sudden load

* **Stress Testing (Login, Materials, Live Sessions)**

  * Gradually increased virtual users to identify system breaking points
  * Measured performance degradation of ASP.NET Core services under load
  * Determined maximum capacity limits of each endpoint

* **Performance Analysis of API Endpoints**

  * Tested three critical ASP.NET Core endpoints:
    * `/api/v1/auth/login`
    * `/api/v1/student/courses/{courseId}/materials`
    * `/api/v1/student/courses/{courseId}/live-sessions`
  * Compared behavior of:
    * Write-heavy operations (Login – authentication, JWT generation)
    * Read-heavy operations (Materials, Live Sessions)

* **Custom Metrics Implementation**

  * Implemented K6 metrics to track:
    * Response time (avg, min, max, p95)
    * Failure rates
    * Endpoint-specific performance trends

* **Performance Bottleneck Identification**

  * **Login endpoint** (ASP.NET Core + Azure SQL):
    * High failure rate (~81% at 100 users)
    * Azure SQL connection pool exhaustion
    * Performance affected by password hashing and JWT generation
  * **Materials endpoint**:
    * 0% failure rate under load
    * Highly optimized read operations using database queries
  * **Live Sessions endpoint**:
    * No failures but higher response time
    * Slower due to complex database joins across multiple tables

### 📊 Impact

* Identified critical performance bottlenecks in ASP.NET Core authentication flow
* Provided insights for database optimization and connection pooling
* Improved understanding of system scalability limits
* Validated API reliability under real-world load conditions
* Ensured stability of high-traffic student features

### ▶️ How to Run (K6 Tests Only)

```bash
# Run ASP.NET Core backend first
cd LMS-Backend
dotnet run

# Run K6 tests
cd LMS-Backend.Tests/K6-tests/Login
k6 run spike-test.js
k6 run stress-test.js

cd ../Materials
k6 run spike-test.js
k6 run stress-test.js

cd ../LiveSessions
k6 run spike-test.js
k6 run stress-test.js
```