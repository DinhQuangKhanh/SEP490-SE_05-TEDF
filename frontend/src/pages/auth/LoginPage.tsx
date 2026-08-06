import { useEffect, useState } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { motion } from "framer-motion";
import { useAuth } from "@/contexts/AuthContext";

const isEmulatorMode = import.meta.env.VITE_USE_FIREBASE_EMULATOR === "true";

/** Emulator quick-login: role keys are the literal seed email prefixes (`{role}{serial}@fpt.edu.vn`). */
const LOGIN_ROLES = [
  { key: "student", label: "Sinh viên" },
  { key: "admin", label: "Quản trị" },
  { key: "lecturer", label: "Giảng viên" },
] as const;

type LoginRole = (typeof LOGIN_ROLES)[number]["key"];

const DEFAULT_PASSWORD = "Test@123456";

const roleHomeMap: Record<string, string> = {
  admin: "/admin",
  mentor: "/lecturer",
  evaluator: "/lecturer",
  student: "/student",
  departmenthead: "/lecturer",
};

/** Compute destination: use `from` state if it matches user role, otherwise go to role home. */
function getRedirectPath(role: string, fromPath?: string): string {
  const roleHome = roleHomeMap[role] ?? "/login";
  // If the saved "from" path belongs to the user's role section, respect it
  if (fromPath && fromPath.startsWith(roleHome)) {
    return fromPath;
  }
  return roleHome;
}

export function LoginPage() {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");
  const [showEmailLogin] = useState(isEmulatorMode);
  const [role, setRole] = useState<LoginRole>("student");
  const [serial, setSerial] = useState("");

  // Real-Firebase email/password credentials (the emulator form above builds its own email).
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);

  const { user, loginWithGoogle, loginWithEmailPassword } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const from = (location.state as { from?: { pathname: string } })?.from?.pathname;

  const handleGoogleLogin = async () => {
    setError("");
    setIsLoading(true);

    try {
      const success = await loginWithGoogle();
      if (success) {
        // Role is set after login — read from auth context won't be updated yet,
        // so we infer role from the user object that will be set by AuthContext.
        // Use a small delay to allow state to settle, then redirect.
        // The actual redirect will happen via useEffect below.
      } else {
        setError("Đăng nhập thất bại. Vui lòng thử lại.");
      }
    } catch {
      setError("Đã có lỗi xảy ra khi đăng nhập");
    } finally {
      setIsLoading(false);
    }
  };

  const handleEmailLogin = async (e: React.SubmitEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError("");

    const trimmed = serial.trim();
    if (!trimmed) {
      setError("Vui lòng nhập số thứ tự tài khoản.");
      return;
    }

    setIsLoading(true);

    try {
      const email = `${role}${trimmed}@fpt.edu.vn`;
      const success = await loginWithEmailPassword(email, DEFAULT_PASSWORD);
      if (success) {
        // Redirect handled by useEffect below when user state updates
      } else {
        setError("Email hoặc mật khẩu không đúng.");
      }
    } catch {
      setError("Đã có lỗi xảy ra khi đăng nhập");
    } finally {
      setIsLoading(false);
    }
  };

  const handleCredentialsLogin = async (e: React.SubmitEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError("");

    const trimmedEmail = email.trim();
    if (!trimmedEmail || !password) {
      setError("Vui lòng nhập email và mật khẩu.");
      return;
    }

    setIsLoading(true);

    try {
      const success = await loginWithEmailPassword(trimmedEmail, password);
      if (!success) {
        setError("Email hoặc mật khẩu không đúng.");
      }
      // On success the redirect happens in the effect below, once the user state settles.
    } catch {
      setError("Đã có lỗi xảy ra khi đăng nhập");
    } finally {
      setIsLoading(false);
    }
  };

  // Redirect after login only as a side-effect to avoid state updates during render.
  useEffect(() => {
    if (!user) return;
    const dest = getRedirectPath(user.role, from);
    navigate(dest, { replace: true });
  }, [user, from, navigate]);

  return (
    <div className="relative flex flex-col items-center justify-center min-h-screen p-4 overflow-hidden font-display academic-pattern text-slate-800 selection:bg-primary/20 selection:text-primary-dark">
      {/* Background decorations */}
      <div className="absolute top-[-10%] left-[-5%] w-[400px] h-[400px] bg-primary/5 rounded-full blur-[80px] pointer-events-none" />
      <div className="absolute bottom-[-10%] right-[-5%] w-[300px] h-[300px] bg-primary/10 rounded-full blur-[60px] pointer-events-none" />

      <motion.main
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5, ease: "easeOut" }}
        className="relative w-full max-w-[440px] bg-white rounded-2xl shadow-soft flex flex-col overflow-hidden border border-white/50 backdrop-blur-sm"
      >
        {/* Header */}
        <motion.div
          initial={{ opacity: 0, y: -10 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.2, duration: 0.4 }}
          className="flex flex-col items-center px-8 pt-10 pb-6 text-center"
        >
          <div className="flex items-center justify-center w-16 h-16 mb-5 border shadow-sm bg-gradient-to-br from-primary/10 to-primary/20 text-primary rounded-xl border-primary/10">
            <span className="text-4xl material-symbols-outlined">school</span>
          </div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-800">Chào mừng đến TEDF</h1>
          <p className="mt-2 text-sm font-medium text-slate-500">Hệ thống quản lý thẩm định đề tài tốt nghiệp</p>
          {isEmulatorMode && (
            <span className="mt-2 inline-block px-2 py-0.5 text-[10px] font-bold uppercase tracking-wider bg-amber-100 text-amber-700 rounded-full border border-amber-200">
              Emulator Mode
            </span>
          )}
        </motion.div>

        {/* Login Content */}
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ delay: 0.3, duration: 0.4 }}
          className="flex flex-col gap-5 p-8 pt-2"
        >
          {error && (
            <motion.div
              initial={{ opacity: 0, height: 0 }}
              animate={{ opacity: 1, height: "auto" }}
              className="p-3 text-sm text-center border rounded-lg bg-error/10 border-error/20 text-error"
            >
              {error}
            </motion.div>
          )}

          {/* Info Box / Google Button — swap based on mode */}
          {isEmulatorMode ? (
            <div className="p-4 border bg-amber-50 border-amber-200 rounded-xl">
              <div className="flex items-start gap-3">
                <span className="material-symbols-outlined text-amber-500 text-xl mt-0.5">science</span>
                <div>
                  <p className="text-sm font-medium text-amber-700">Emulator Mode — dùng Email/Password</p>
                  <p className="mt-1 text-xs text-amber-600">
                    Google Sign-In không khả dụng với tài khoản test. Hãy đăng nhập bằng email &amp; mật khẩu bên dưới.
                  </p>
                </div>
              </div>
            </div>
          ) : (
            <>
              <div className="p-4 border bg-primary/5 border-primary/10 rounded-xl">
                <div className="flex items-start gap-3">
                  <span className="material-symbols-outlined text-primary text-xl mt-0.5">info</span>
                  <div>
                    <p className="text-sm font-medium text-slate-700">Sử dụng tài khoản Google của trường</p>
                    <p className="mt-1 text-xs text-slate-500">
                      Đăng nhập bằng email @fpt.edu.vn được cấp bởi nhà trường để truy cập hệ thống.
                    </p>
                  </div>
                </div>
              </div>

              {/* Google Login Button */}
              <motion.button
                whileHover={{ y: -2, boxShadow: "0 8px 25px rgba(66, 133, 244, 0.25)" }}
                whileTap={{ scale: 0.98 }}
                onClick={handleGoogleLogin}
                disabled={isLoading}
                className="flex w-full items-center justify-center gap-3 rounded-xl bg-white hover:bg-slate-50 border-2 border-slate-200 hover:border-[#4285F4] text-slate-700 font-semibold h-14 transition-all disabled:opacity-70 disabled:cursor-not-allowed group"
              >
                {isLoading ? (
                  <span className="material-symbols-outlined animate-spin text-[#4285F4]">progress_activity</span>
                ) : (
                  <>
                    <svg className="w-6 h-6" viewBox="0 0 24 24">
                      <path
                        d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"
                        fill="#4285F4"
                      />
                      <path
                        d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
                        fill="#34A853"
                      />
                      <path
                        d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"
                        fill="#FBBC05"
                      />
                      <path
                        d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"
                        fill="#EA4335"
                      />
                    </svg>
                    <span className="text-base">Đăng nhập với Google</span>
                  </>
                )}
              </motion.button>
            </>
          )}

          {/* Email/Password Login against the real Firebase project */}
          {!isEmulatorMode && (
            <>
              <div className="flex items-center gap-3">
                <span className="h-px flex-1 bg-slate-200" />
                <span className="text-xs font-medium text-slate-400">hoặc</span>
                <span className="h-px flex-1 bg-slate-200" />
              </div>

              <form onSubmit={handleCredentialsLogin} className="flex flex-col gap-3">
                <label className="flex flex-col gap-1.5">
                  <span className="text-xs font-semibold text-slate-600">Email</span>
                  <input
                    type="email"
                    autoComplete="username"
                    placeholder="you@fpt.edu.vn"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    className="w-full px-4 text-sm border rounded-lg h-11 border-slate-200 focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary/30"
                  />
                </label>

                <label className="flex flex-col gap-1.5">
                  <span className="text-xs font-semibold text-slate-600">Mật khẩu</span>
                  <div className="relative">
                    <input
                      type={showPassword ? "text" : "password"}
                      autoComplete="current-password"
                      placeholder="••••••••"
                      value={password}
                      onChange={(e) => setPassword(e.target.value)}
                      className="w-full pl-4 pr-11 text-sm border rounded-lg h-11 border-slate-200 focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary/30"
                    />
                    <button
                      type="button"
                      onClick={() => setShowPassword((v) => !v)}
                      aria-label={showPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu"}
                      className="absolute -translate-y-1/2 right-3 top-1/2 text-slate-400 hover:text-slate-600"
                    >
                      <span className="material-symbols-outlined text-[20px]">
                        {showPassword ? "visibility_off" : "visibility"}
                      </span>
                    </button>
                  </div>
                </label>

                <button
                  type="submit"
                  disabled={isLoading}
                  className="w-full font-semibold text-white transition-colors rounded-lg h-11 bg-primary hover:bg-primary/90 disabled:opacity-70 disabled:cursor-not-allowed"
                >
                  {isLoading ? "Đang đăng nhập..." : "Đăng nhập bằng Email"}
                </button>
              </form>
            </>
          )}

          {/* Email/Password Login — only visible in emulator mode */}
          {isEmulatorMode && (
            <>
              {showEmailLogin && (
                <motion.form
                  initial={{ opacity: 0, height: 0 }}
                  animate={{ opacity: 1, height: "auto" }}
                  onSubmit={handleEmailLogin}
                  className="flex flex-col gap-3 p-4 border bg-amber-50 border-amber-200 rounded-xl"
                >
                  <p className="text-xs font-medium text-amber-700">Firebase Emulator - Test Login</p>

                  {/* Role selector */}
                  <div className="grid grid-cols-3 gap-2">
                    {LOGIN_ROLES.map((r) => (
                      <button
                        key={r.key}
                        type="button"
                        onClick={() => setRole(r.key)}
                        className={`h-9 rounded-lg text-xs font-semibold transition-colors ${
                          role === r.key
                            ? "bg-primary text-white"
                            : "border border-slate-200 text-slate-600 hover:bg-slate-50"
                        }`}
                      >
                        {r.label}
                      </button>
                    ))}
                  </div>

                  {/* Serial number */}
                  <input
                    type="text"
                    inputMode="numeric"
                    placeholder="Số thứ tự (vd: 1)"
                    value={serial}
                    onChange={(e) => setSerial(e.target.value)}
                    className="w-full px-4 text-sm border rounded-lg h-11 border-slate-200 focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary/30"
                  />

                  {/* Live email preview */}
                  <p className="text-[11px] text-amber-700">
                    Đăng nhập với:{" "}
                    <span className="font-semibold">{`${role}${serial.trim() || "…"}@fpt.edu.vn`}</span>
                  </p>

                  <button
                    type="submit"
                    disabled={isLoading}
                    className="w-full font-semibold text-white transition-colors rounded-lg h-11 bg-amber-500 hover:bg-amber-600 disabled:opacity-70"
                  >
                    {isLoading ? "Đang đăng nhập..." : "Đăng nhập (Emulator)"}
                  </button>
                  <p className="text-[10px] text-amber-600">Mật khẩu mặc định: Test@123456</p>
                </motion.form>
              )}
            </>
          )}

          {/* Help text */}
          <p className="mt-2 text-xs text-center text-slate-400">
            Nếu gặp vấn đề đăng nhập, vui lòng liên hệ bộ phận hỗ trợ kỹ thuật
          </p>
        </motion.div>

        {/* Bottom accent */}
        <div className="h-1.5 w-full bg-gradient-to-r from-primary via-[#5eead4] to-primary" />
      </motion.main>

      {/* Footer */}
      <motion.footer
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        transition={{ delay: 0.5, duration: 0.4 }}
        className="relative z-10 mt-8 text-center"
      >
        <p className="text-xs font-medium text-slate-500">
          Hỗ trợ kỹ thuật:{" "}
          <a className="underline transition-colors hover:text-primary" href="#">
            support@university.edu.vn
          </a>
        </p>
        <p className="text-slate-400 text-[10px] mt-2">© 2026 University Graduation Project Management System</p>
      </motion.footer>
    </div>
  );
}
