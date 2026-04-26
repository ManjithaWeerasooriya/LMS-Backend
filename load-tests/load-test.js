import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { SharedArray } from 'k6/data';

const BASE_URL = (__ENV.BASE_URL || 'http://localhost:5251').replace(/\/+$/, '');

const users = new SharedArray('users', function () {
  return open('./users.csv')
    .split(/\r?\n/)
    .slice(1)
    .filter(line => line.trim() !== '')
    .map(line => {
      const [email, password, role, deviceId] = line.split(',');
      return {
        email: email.trim(),
        password: password.trim(),
        role: role.trim().toLowerCase(),
        deviceId: deviceId.trim(),
      };
    });
});

const studentUser = users.find(user => user.role === 'student');
const teacherUser = users.find(user => user.role === 'teacher');

export const options = {
  scenarios: {
    public_browse: {
      executor: 'constant-vus',
      exec: 'publicBrowse',
      vus: Number(__ENV.PUBLIC_VUS || 1),
      duration: __ENV.DURATION || '1m',
    },
    student_flow: {
      executor: 'constant-vus',
      exec: 'studentFlow',
      vus: Number(__ENV.STUDENT_VUS || 2),
      duration: __ENV.DURATION || '1m',
    },
    teacher_flow: {
      executor: 'constant-vus',
      exec: 'teacherFlow',
      vus: Number(__ENV.TEACHER_VUS || 1),
      duration: __ENV.DURATION || '1m',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.2'],
    http_req_duration: ['p(95)<6000'],
    checks: ['rate>0.95'],
  },
};

export function publicBrowse() {
  group('public endpoints', function () {
    const statsRes = http.get(`${BASE_URL}/api/public/stats`);
    check(statsRes, {
      'public stats is 200': res => res.status === 200,
    });

    const coursesRes = http.get(`${BASE_URL}/api/public/courses`);
    check(coursesRes, {
      'public courses is 200': res => res.status === 200,
    });

    const courses = parseJson(coursesRes) || [];
    const firstCourse = courses[0];

    if (firstCourse?.id) {
      const detailRes = http.get(`${BASE_URL}/api/public/courses/${firstCourse.id}`);
      check(detailRes, {
        'public course detail is 200': res => res.status === 200,
      });
    }
  });

  sleep(1);
}

export function studentFlow() {
  if (!studentUser) {
    return;
  }

  const token = login(studentUser);
  if (!token) {
    return;
  }

  group('student endpoints', function () {
    const meRes = http.get(`${BASE_URL}/api/v1/users/me`, authParams(token));
    check(meRes, {
      'student profile is 200': res => res.status === 200,
    });

    const dashboardRes = http.get(`${BASE_URL}/api/v1/student/dashboard`, authParams(token));
    check(dashboardRes, {
      'student dashboard is 200': res => res.status === 200,
    });

    const myCoursesRes = http.get(`${BASE_URL}/api/v1/student/courses/my`, authParams(token));
    check(myCoursesRes, {
      'student my courses is 200': res => res.status === 200,
    });

    const quizzesRes = http.get(`${BASE_URL}/api/v1/student/quizzes`, authParams(token));
    check(quizzesRes, {
      'student quizzes is 200': res => res.status === 200,
    });

    const myCoursesPayload = parseJson(myCoursesRes);
    const myCourses = myCoursesPayload?.data || myCoursesPayload || [];
    const firstCourse = myCourses[0];

    if (firstCourse?.id) {
      const materialsRes = http.get(
        `${BASE_URL}/api/v1/student/courses/${firstCourse.id}/materials`,
        authParams(token)
      );

      check(materialsRes, {
        'student materials is 200': res => res.status === 200,
      });
    }

    const quizzesPayload = parseJson(quizzesRes);
    const quizzes = quizzesPayload?.data || quizzesPayload || [];
    const firstQuiz = quizzes[0];

    if (firstQuiz?.quizId) {
      const quizDetailRes = http.get(
        `${BASE_URL}/api/v1/student/quizzes/${firstQuiz.quizId}`,
        authParams(token)
      );

      check(quizDetailRes, {
        'student quiz detail is 200': res => res.status === 200,
      });
    }
  });

  sleep(1);
}

export function teacherFlow() {
  if (!teacherUser) {
    return;
  }

  const token = login(teacherUser);
  if (!token) {
    return;
  }

  group('teacher endpoints', function () {
    const meRes = http.get(`${BASE_URL}/api/v1/users/me`, authParams(token));
    check(meRes, {
      'teacher profile is 200': res => res.status === 200,
    });

    const dashboardRes = http.get(`${BASE_URL}/api/v1/teacher/dashboard`, authParams(token));
    check(dashboardRes, {
      'teacher dashboard is 200': res => res.status === 200,
    });

    const coursesRes = http.get(`${BASE_URL}/api/v1/teacher/courses`, authParams(token));
    check(coursesRes, {
      'teacher courses is 200': res => res.status === 200,
    });

    const reportRes = http.get(`${BASE_URL}/api/v1/admin/reports/overview`, authParams(token));
    check(reportRes, {
      'teacher report overview is 200': res => res.status === 200,
    });

    const courses = parseJson(coursesRes) || [];
    const firstCourse = courses[0];

    if (firstCourse?.id) {
      const courseDetailRes = http.get(
        `${BASE_URL}/api/v1/teacher/courses/${firstCourse.id}`,
        authParams(token)
      );

      check(courseDetailRes, {
        'teacher course detail is 200': res => res.status === 200,
      });

      const courseQuizzesRes = http.get(
        `${BASE_URL}/api/v1/teacher/quizzes/course/${firstCourse.id}`,
        authParams(token)
      );

      check(courseQuizzesRes, {
        'teacher course quizzes is 200': res => res.status === 200,
      });
    }
  });

  sleep(1);
}

function login(user) {
  const loginRes = http.post(
    `${BASE_URL}/api/v1/auth/login`,
    JSON.stringify({
      email: user.email,
      password: user.password,
      deviceId: user.deviceId,
    }),
    {
      headers: {
        'Content-Type': 'application/json',
      },
    }
  );

  const loginBody = parseJson(loginRes);
  const accessToken = loginBody?.accessToken;

  check(loginRes, {
    [`${user.role} login is 200`]: res => res.status === 200,
    [`${user.role} access token exists`]: () => Boolean(accessToken),
  });

  return accessToken || null;
}

function authParams(token) {
  return {
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
  };
}

function parseJson(response) {
  try {
    return response.json();
  } catch (_) {
    return null;
  }
}
