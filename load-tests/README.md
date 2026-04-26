# Simple k6 Load Test

This version is intentionally simplified for viva explanation. It keeps only the most important LMS flows and uses the two accounts currently in your database.

## Scenarios
1. `public_browse`
   Tests anonymous endpoints:
   - `GET /api/public/stats`
   - `GET /api/public/courses`
   - `GET /api/public/courses/{id}`

2. `student_flow`
   Tests a logged-in student:
   - `POST /api/v1/auth/login`
   - `GET /api/v1/users/me`
   - `GET /api/v1/student/dashboard`
   - `GET /api/v1/student/courses/my`
   - `GET /api/v1/student/quizzes`
   - `GET /api/v1/student/courses/{courseId}/materials`
   - `GET /api/v1/student/quizzes/{quizId}`

3. `teacher_flow`
   Tests a logged-in teacher:
   - `POST /api/v1/auth/login`
   - `GET /api/v1/users/me`
   - `GET /api/v1/teacher/dashboard`
   - `GET /api/v1/teacher/courses`
   - `GET /api/v1/teacher/courses/{courseId}`
   - `GET /api/v1/teacher/quizzes/course/{courseId}`
   - `GET /api/v1/admin/reports/overview`

## Users

```csv
email,password,role,deviceId
ishanchathuranga626@gmail.com,@IshaN2002,student,student-device-01
admin@lms.local,Admin123!,teacher,teacher-device-01
```

## Why This Version Is Easier To Explain
- Only 3 scenarios
- Only 2 real users
- No refresh-token logic
- No CSV overrides for course and quiz IDs
- IDs are taken from API responses at runtime

## Run

From `LMS-Backend`:

```powershell
k6 run .\load-tests\load-test.js
```

Small viva demo:

```powershell
k6 run -e DURATION=30s -e PUBLIC_VUS=1 -e STUDENT_VUS=1 -e TEACHER_VUS=1 .\load-tests\load-test.js
```

Normal assignment run:

```powershell
k6 run -e DURATION=1m -e PUBLIC_VUS=1 -e STUDENT_VUS=2 -e TEACHER_VUS=1 .\load-tests\load-test.js
```

## Environment Variables
- `BASE_URL`: default is `http://localhost:5251`
- `DURATION`: default is `1m`
- `PUBLIC_VUS`: default is `1`
- `STUDENT_VUS`: default is `2`
- `TEACHER_VUS`: default is `1`

## Simple Explanation For Viva
- Public scenario checks how the system behaves for guest users.
- Student scenario checks authenticated student features such as dashboard, courses, materials, and quizzes.
- Teacher scenario checks authenticated teacher features such as dashboard, course management view, quizzes, and report overview.
- k6 runs these scenarios in parallel using virtual users, so the backend receives mixed traffic from different endpoint types.
