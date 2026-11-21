import { HttpInterceptorFn } from '@angular/common/http';

function readCookie(name: string): string | null {
  const match = document.cookie.match(new RegExp(`(?:^|; )${name.replace(/([.$?*|{}()[\]\\/+^])/g, '\\$1')}=([^;]*)`));
  return match ? decodeURIComponent(match[1]) : null;
}

export const csrfInterceptor: HttpInterceptorFn = (req, next) => {
  if (!/^(POST|PUT|PATCH|DELETE)$/i.test(req.method)) {
    return next(req);
  }

  const token = readCookie('XSRF-TOKEN');
  if (!token) {
    return next(req);
  }

  return next(
    req.clone({
      setHeaders: {
        'X-XSRF-TOKEN': token
      },
      withCredentials: true
    })
  );
};
