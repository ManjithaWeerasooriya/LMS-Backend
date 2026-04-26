# SE3112 – Testing Implementation
## K6 Load Testing — Student 3

### Tool: K6 (Load/Performance Testing)
### Category: Load/Performance Testing

---

## Project: LMS Backend (ASP.NET Core + Azure SQL)

The existing codebase is the LMS (Learning Management System) backend built with
ASP.NET Core and Azure SQL Database. K6 is used to perform load testing on three
critical API endpoints to measure system behaviour under spike and stress conditions.

---

## Folder Structure
K6-tests/
├── Login/
│   ├── spike-test.js         — Test 1: Spike testing on auth login endpoint
│   └── stress-test.js        — Test 2: Stress testing on auth login endpoint
├── Materials/
│   ├── spike-test.js         — Test 3: Spike testing on student materials endpoint
│   └── stress-test.js        — Test 4: Stress testing on student materials endpoint
└── LiveSessions/
├── spike-test.js         — Test 5: Spike testing on student live sessions endpoint
└── stress-test.js        — Test 6: Stress testing on student live sessions endpoint

---

## Student Ownership

| Test | Feature | File | Owner |
|------|---------|------|-------|
| 1 | Spike Testing — Login | K6-tests/Login/spike-test.js | Student 3 — Deepana Nirmal |
| 2 | Stress Testing — Login | K6-tests/Login/stress-test.js | Student 3 — Deepana Nirmal |
| 3 | Spike Testing — Materials | K6-tests/Materials/spike-test.js | Student 3 — Deepana Nirmal |
| 4 | Stress Testing — Materials | K6-tests/Materials/stress-test.js | Student 3 — Deepana Nirmal |
| 5 | Spike Testing — Live Sessions | K6-tests/LiveSessions/spike-test.js | Student 3 — Deepana Nirmal |
| 6 | Stress Testing — Live Sessions | K6-tests/LiveSessions/stress-test.js | Student 3 — Deepana Nirmal |

---

## Endpoints Tested

| Endpoint | Method | Auth | Tests |
|----------|--------|------|-------|
| `/api/v1/auth/login` | POST | None | Spike + Stress |
| `/api/v1/student/courses/{courseId}/materials` | GET | Bearer JWT (Student) | Spike + Stress |
| `/api/v1/student/courses/{courseId}/live-sessions` | GET | Bearer JWT (Student) | Spike + Stress |

---

## Test Cases

### Test 1 — Login Spike Test
**File:** `K6-tests/Login/spike-test.js`
**Endpoint:** `POST /api/v1/auth/login`
**Scenario:** Simulates a sudden surge of 100 users attempting to log in simultaneously
within 5 seconds — mimicking a university portal on the first day of semester.

**Stages:**
| Stage | Duration | VUs | Description |
|-------|----------|-----|-------------|
| Baseline | 15s | 0 | No load — system idle |
| Spike | 5s | 0 → 100 | Sudden surge |
| Hold | 20s | 100 | Sustained spike |
| Recovery | 5s | 100 → 0 | Load removed |

**Thresholds:** p(95) < 2000ms, failure rate < 10%
**Result:** 81% failure rate — Azure SQL connection pool exhausted under sudden load

---

### Test 2 — Login Stress Test
**File:** `K6-tests/Login/stress-test.js`
**Endpoint:** `POST /api/v1/auth/login`
**Scenario:** Gradually increases concurrent users to find the system's breaking point
for the login endpoint.

**Stages:**
| Stage | Duration | VUs | Description |
|-------|----------|-----|-------------|
| Warm up | 20s | 0 → 20 | Light load |
| Moderate | 20s | 20 → 50 | Medium load |
| High | 20s | 50 → 100 | Heavy load |
| Breaking point | 20s | 100 → 150 | Beyond capacity |
| Ramp down | 10s | 150 → 0 | Load removed |

**Thresholds:** p(95) < 3000ms, failure rate < 20%
**Result:** Breaking point observed at ~100 VUs — median 1.75s at low load, 
degrades to 50s max at 150 VUs

---

### Test 3 — Materials Spike Test
**File:** `K6-tests/Materials/spike-test.js`
**Endpoint:** `GET /api/v1/student/courses/{courseId}/materials`
**Scenario:** Simulates 100 enrolled students simultaneously opening their course
materials page — a common scenario after a teacher uploads new content.

**Stages:**
| Stage | Duration | VUs | Description |
|-------|----------|-----|-------------|
| Baseline | 15s | 0 | No load |
| Spike | 5s | 0 → 100 | Sudden surge |
| Hold | 20s | 100 | Sustained spike |
| Recovery | 5s | 100 → 0 | Load removed |

**Thresholds:** p(95) < 5000ms, failure rate < 10%
**Result:** All thresholds passed — 0% failure rate, p(95) = 1.21s,
median 236ms. Much more resilient than login due to simple DB read.

---

### Test 4 — Materials Stress Test
**File:** `K6-tests/Materials/stress-test.js`
**Endpoint:** `GET /api/v1/student/courses/{courseId}/materials`
**Scenario:** Gradually increases load on the materials endpoint to find its
maximum concurrent user capacity.

**Stages:**
| Stage | Duration | VUs | Description |
|-------|----------|-----|-------------|
| Warm up | 20s | 0 → 20 | Light load |
| Moderate | 20s | 20 → 50 | Medium load |
| High | 20s | 50 → 100 | Heavy load |
| Breaking point | 20s | 100 → 150 | Beyond capacity |
| Ramp down | 10s | 150 → 0 | Load removed |

**Thresholds:** p(95) < 5000ms, failure rate < 20%

---

### Test 5 — Live Sessions Spike Test
**File:** `K6-tests/LiveSessions/spike-test.js`
**Endpoint:** `GET /api/v1/student/courses/{courseId}/live-sessions`
**Scenario:** Simulates 100 students simultaneously checking their live session
schedule — typical before a scheduled class begins.

**Stages:**
| Stage | Duration | VUs | Description |
|-------|----------|-----|-------------|
| Baseline | 15s | 0 | No load |
| Spike | 5s | 0 → 100 | Sudden surge |
| Hold | 20s | 100 | Sustained spike |
| Recovery | 5s | 100 → 0 | Load removed |

**Thresholds:** p(95) < 8000ms, failure rate < 10%
**Result:** 0% failure rate — all requests returned 200. Slower than materials
(p(95) = 6.34s) due to complex DB joins across sessions, enrollments and courses.

---

### Test 6 — Live Sessions Stress Test
**File:** `K6-tests/LiveSessions/stress-test.js`
**Endpoint:** `GET /api/v1/student/courses/{courseId}/live-sessions`
**Scenario:** Gradually increases load on the live sessions endpoint to find its
maximum capacity and identify performance degradation patterns.

**Stages:**
| Stage | Duration | VUs | Description |
|-------|----------|-----|-------------|
| Warm up | 20s | 0 → 20 | Light load |
| Moderate | 20s | 20 → 50 | Medium load |
| High | 20s | 50 → 100 | Heavy load |
| Breaking point | 20s | 100 → 150 | Beyond capacity |
| Ramp down | 10s | 150 → 0 | Load removed |

**Thresholds:** p(95) < 8000ms, failure rate < 20%

---

## Custom Metrics

| Metric | Type | Used In | Purpose |
|--------|------|---------|---------|
| `login_duration` | Trend | Login tests | Tracks login response time (avg, min, max, p95) |
| `login_fail_rate` | Rate | Login tests | Tracks percentage of failed login requests |
| `materials_duration` | Trend | Materials tests | Tracks materials response time |
| `materials_fail_rate` | Rate | Materials tests | Tracks percentage of failed materials requests |
| `sessions_duration` | Trend | LiveSessions tests | Tracks live sessions response time |
| `sessions_fail_rate` | Rate | LiveSessions tests | Tracks percentage of failed session requests |

---

## Key Findings

| Endpoint | Spike Result | Breaking Point |
|----------|-------------|----------------|
| Login | 81% failure at 100 VUs | ~20 VUs |
| Materials | 0% failure at 100 VUs | > 150 VUs |
| Live Sessions | 0% failure at 100 VUs | > 100 VUs |

- Login is the weakest endpoint — password hashing + JWT generation + DB writes
  under concurrent load exhausts Azure SQL connection pool rapidly
- Materials GET is the most resilient — simple read query with no write operations
- Live Sessions are slower than Materials due to complex multi-table DB joins
- All GET endpoints handle spike load cleanly with 0% failure rate

---

## How to Run

### Prerequisites
```bash
brew install k6        # macOS
```

### Start the backend first
```bash
cd LMS-Backend
dotnet run
# wait for: Now listening on: http://localhost:5251
```

### Run all tests
```bash
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