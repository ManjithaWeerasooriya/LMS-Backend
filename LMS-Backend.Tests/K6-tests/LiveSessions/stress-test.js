import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Rate } from 'k6/metrics';

const sessionsDuration = new Trend('sessions_duration');
const sessionsFailRate = new Rate('sessions_fail_rate');

const COURSE_ID = '307e455e-3990-4919-b9e4-0e89e0939860';

export const options = {
  stages: [
    { duration: '20s', target: 20 },
    { duration: '20s', target: 50 },
    { duration: '20s', target: 100 },
    { duration: '20s', target: 150 },
    { duration: '10s', target: 0 },
  ],

  thresholds: {
    http_req_duration: ['p(95)<8000'],
    http_req_failed: ['rate<0.20'],
    sessions_fail_rate: ['rate<0.20'],
  },
};

const BASE_URL = 'http://localhost:5251';

/**
 * LOGIN ONCE PER VU (correct k6 pattern)
 */
export function setup() {
  const payload = JSON.stringify({
    email: 'inuransira@gmail.com',
    password: 'Google@2009',
    deviceId: `setup-device`,
  });

  const params = {
    headers: {
      'Content-Type': 'application/json',
    },
  };

  const res = http.post(`${BASE_URL}/api/v1/auth/login`, payload, params);

  if (res.status !== 200) {
    throw new Error(`Login failed in setup: ${res.status} - ${res.body}`);
  }

  const token = res.json('accessToken');

  if (!token) {
    throw new Error(`No access token received: ${res.body}`);
  }

  return { token };
}

export default function (data) {
  const params = {
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${data.token}`,
    },
  };

  const res = http.get(
    `${BASE_URL}/api/v1/student/courses/${COURSE_ID}/live-sessions`,
    params
  );

  sessionsDuration.add(res.timings.duration);
  sessionsFailRate.add(res.status !== 200);

  check(res, {
    'status is 200': (r) => r.status === 200,
    'has sessions data': (r) => {
      try {
        return r.json('data') !== null;
      } catch {
        return false;
      }
    },
    'response time < 5s': (r) => r.timings.duration < 5000,
  });

  sleep(1);
}