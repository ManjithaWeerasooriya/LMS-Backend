# LMS Backend k6 Load Test

This folder contains a simplified `k6` load test for the LMS backend. It was designed focuses on three key requirements:

- data-driven load testing
- multiple requests
- varied endpoints

## Why This Fits The Requirement

### 1. Data-Driven Load Testing
This test is data-driven because user information is read from [users.csv](./users.csv), not hardcoded into the test logic.

The CSV file contains:
- user email
- password
- role
- device ID

Current roles used:
- `student`
- `teacher`

Because the script reads the user records from the CSV file, the test behavior depends on input data. This is the core idea of data-driven testing.

### 2. Multiple Requests
Each scenario sends multiple API requests, not just one request.

For example:
- the public scenario sends course and statistics requests
- the student scenario logs in and then requests profile, dashboard, courses, quizzes, and materials
- the teacher scenario logs in and then requests profile, dashboard, courses, quizzes, and reports

This makes the test closer to real user behavior.

### 3. Varied Endpoints
The test covers different API groups in the backend:

- `/api/public/...`
- `/api/v1/users/...`
- `/api/v1/student/...`
- `/api/v1/teacher/...`
- `/api/v1/admin/...`

So it does not test only one route. It tests different backend modules such as:
- public browsing
- authentication
- dashboards
- courses
- materials
- quizzes
- reports

## Test Scenarios

The script contains 3 scenarios.

### 1. Public Browse
This simulates an anonymous user.

Requests:
- `GET /api/public/stats`
- `GET /api/public/courses`
- `GET /api/public/courses/{id}`

### 2. Student Flow
This simulates a logged-in student.

Requests:
- `POST /api/v1/auth/login`
- `GET /api/v1/users/me`
- `GET /api/v1/student/dashboard`
- `GET /api/v1/student/courses/my`
- `GET /api/v1/student/quizzes`
- `GET /api/v1/student/courses/{courseId}/materials`
- `GET /api/v1/student/quizzes/{quizId}`

### 3. Teacher Flow
This simulates a logged-in teacher.

Requests:
- `POST /api/v1/auth/login`
- `GET /api/v1/users/me`
- `GET /api/v1/teacher/dashboard`
- `GET /api/v1/teacher/courses`
- `GET /api/v1/teacher/courses/{courseId}`
- `GET /api/v1/teacher/quizzes/course/{courseId}`
- `GET /api/v1/admin/reports/overview`

## Test Data

The input data is stored in [users.csv](./users.csv).

Example format:

```csv
email,password,role,deviceId
ishanchathuranga626@gmail.com,@IshaN2002,student,student-device-01
admin@lms.local,Admin123!,teacher,teacher-device-01
```

Description of columns:
- `email`: account email
- `password`: account password
- `role`: role used in the test
- `deviceId`: required by the backend login API

## How The Script Works

1. `k6` reads the users from `users.csv`.
2. The public scenario runs without login.
3. The student scenario logs in using the student record from the CSV.
4. The teacher scenario logs in using the teacher record from the CSV.
5. After login, the script sends additional requests to different LMS endpoints.
6. The script gets course IDs and quiz IDs dynamically from previous API responses when needed.

## Prerequisites

Before running the test:

1. Make sure the backend API is running.
2. Make sure the database connection in `.env` is correct.
3. Make sure the student and teacher accounts in `users.csv` exist in the database.
4. Make sure the accounts are active and can log in successfully.
5. Make sure `k6` is installed.

## Default Backend URL

The test uses this backend URL by default:

```text
http://localhost:5251
```

If needed, you can override it with the `BASE_URL` environment variable.

## How To Run

Open PowerShell in the `LMS-Backend` folder and run:

```powershell
k6 run .\load-tests\load-test.js
```

## Demo Run

This is a small run for demonstration or viva:

```powershell
k6 run -e DURATION=30s -e PUBLIC_VUS=1 -e STUDENT_VUS=1 -e TEACHER_VUS=1 .\load-tests\load-test.js
```

## Normal Assignment Run

This is a balanced run for the assignment:

```powershell
k6 run -e DURATION=1m -e PUBLIC_VUS=1 -e STUDENT_VUS=1 -e TEACHER_VUS=1 .\load-tests\load-test.js
```

## Heavier Load Run

If you want more public traffic:

```powershell
k6 run -e DURATION=5m -e PUBLIC_VUS=5 -e STUDENT_VUS=1 -e TEACHER_VUS=1 .\load-tests\load-test.js
```

Note:
- public VUs can be increased more safely
- authenticated VUs should be increased carefully when only a small number of user accounts are available

## Environment Variables

The script supports these environment variables:

- `BASE_URL`: backend URL
- `DURATION`: total test duration
- `PUBLIC_VUS`: number of virtual users for public traffic
- `STUDENT_VUS`: number of virtual users for student traffic
- `TEACHER_VUS`: number of virtual users for teacher traffic

## What The Output Means

After the test finishes, `k6` shows:

- `checks`: how many assertions passed
- `http_req_failed`: failed HTTP requests
- `http_req_duration`: request response time
- `iterations`: how many scenario loops completed
- `vus`: number of active virtual users

Good results usually mean:
- high check pass rate
- low failed request rate
- acceptable response time

## Conclusion

This load test is a valid data-driven load testing setup for the LMS backend because:

- it uses input data from a CSV file
- it sends multiple requests in each scenario
- it tests varied endpoints from multiple backend modules
- it simulates three different traffic types: public, student, and teacher

This makes it suitable for demonstrating the required backend load testing concepts in the assignment.
