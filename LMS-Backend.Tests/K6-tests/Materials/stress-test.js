import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Rate } from 'k6/metrics';

const materialsDuration = new Trend('materials_duration');
const materialsFailRate = new Rate('materials_fail_rate');

const COURSE_ID = '307e455e-3990-4919-b9e4-0e89e0939860';

export const options = {
  stages: [
    { duration: '20s', target: 20  },
    { duration: '20s', target: 50  },
    { duration: '20s', target: 100 },
    { duration: '20s', target: 150 },
    { duration: '10s', target: 0   },
  ],
  thresholds: {
    http_req_duration:   ['p(95)<3000'],
    http_req_failed:     ['rate<0.20'],
    materials_fail_rate: ['rate<0.20'],
  },
};

const BASE_URL = 'http://localhost:5251';

export function setup() {
  const res = http.post(`${BASE_URL}/api/v1/auth/login`,
    JSON.stringify({
      email:    'inuransira@gmail.com',
      password: 'Google@2009',
      deviceId: 'k6-materials-stress-setup',
    }),
    { headers: { 'Content-Type': 'application/json' } }
  );

  const token = res.json('accessToken');
  if (!token) throw new Error(`Login failed: ${res.body}`);
  return { token };
}

export default function (data) {
  const params = {
    headers: {
      'Content-Type':  'application/json',
      'Authorization': `Bearer ${data.token}`,
    },
  };

  const res = http.get(
    `${BASE_URL}/api/v1/student/courses/${COURSE_ID}/materials`,
    params
  );

  materialsDuration.add(res.timings.duration);
  materialsFailRate.add(res.status !== 200);

  check(res, {
    'status is 200':       (r) => r.status === 200,
    'has materials data':  (r) => r.json('data') !== null,
    'response time < 3s':  (r) => r.timings.duration < 3000,
  });

  sleep(1);
}