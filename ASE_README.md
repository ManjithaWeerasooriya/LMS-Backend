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


## 👨‍💻 Team Member 4

**Name:** W.G.I.C Premathilaka
**IT Number:** IT23613904

### 🔍 Focus Area

* Data-Driven Load Testing (K6)
* Performance Analysis of ASP.NET Core API Endpoints

### ✅ Contributions

* **Implemented Data-Driven Load Testing Using K6**

  * Created a K6 test suite that reads user credentials and roles from a CSV file
  * Designed the test to simulate realistic LMS traffic using separate student and teacher accounts
  * Ensured the test behavior is driven by input data instead of hardcoded user logic

* **Built Multiple Request User Flows**

  * Developed load test scenarios that execute several API requests in sequence
  * Simulated actual backend usage instead of testing only a single endpoint
  * Included authentication followed by role-based API requests for student and teacher users

* **Tested Varied API Endpoints**

  * Covered multiple backend route groups:
    * `/api/public/...`
    * `/api/v1/users/...`
    * `/api/v1/student/...`
    * `/api/v1/teacher/...`
    * `/api/v1/admin/...`
  * Tested different LMS backend features such as:
    * public browsing
    * authentication
    * dashboards
    * courses
    * materials
    * quizzes
    * reports

* **Implemented Three Main K6 Scenarios**

  * **Public Browse Scenario**
    * Tested anonymous access to platform statistics and public course endpoints
  * **Student Flow Scenario**
    * Tested login, profile, dashboard, enrolled courses, quizzes, and course materials
  * **Teacher Flow Scenario**
    * Tested login, profile, dashboard, courses, course quizzes, and reports overview

* **Performance Metrics and Threshold Validation**

  * Used K6 built-in metrics to monitor:
    * request duration
    * failed request rate
    * check pass rate
  * Applied thresholds to validate:
    * response time performance
    * request reliability
    * overall API stability under load

* **Dynamic Endpoint Testing**

  * Designed the script to retrieve course IDs and quiz IDs dynamically from API responses
  * Reduced hardcoded values in the load test
  * Improved flexibility and realism of the test execution flow

### 📊 Impact

* Demonstrated that the LMS backend supports data-driven load testing
* Validated backend behavior under multiple sequential requests
* Verified system performance across varied API endpoints and user roles
* Improved confidence in the stability of student and teacher backend services
* Provided a simple but effective K6-based load testing solution for the LMS project

### ▶️ How to Run (K6 Tests Only)

```bash
# Run ASP.NET Core backend first
cd LMS-Backend
dotnet run

# Run the K6 load test
k6 run .\load-tests\load-test.js

# Optional small demo run
k6 run -e DURATION=30s -e PUBLIC_VUS=1 -e STUDENT_VUS=1 -e TEACHER_VUS=1 .\load-tests\load-test.js