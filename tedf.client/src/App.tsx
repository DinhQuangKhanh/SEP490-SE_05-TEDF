import { NotFoundPage, AccessDeniedPage } from "@/pages/errors";
import { useEffect } from "react";
import { Routes, Route, Navigate } from "react-router-dom";
import { AnimatePresence } from "framer-motion";
import { AuthProvider, useAuth } from "@/contexts/AuthContext";
import { MaintenanceProvider } from "@/contexts/MaintenanceContext";
import { SystemErrorProvider } from "@/contexts/SystemErrorContext";
import { ProtectedRoute } from "@/components/auth/ProtectedRoute";
import { AdminLayout, EvaluatorLayout, MentorLayout, StudentLayout, DepartmentHeadLayout } from "@/components/layout";
import {
  LoginPage,
  DashboardPage,
  SettingsPage,
  SemestersPage,
  UsersPage,
  ProjectsPage,
  SupportPage,
  EvaluatorDashboardPage,
  EvaluatorProjectsPage,
  EvaluatorHistoryPage,
  EvaluatorReviewPage,
  EvaluatorSimilarityPage,
  EvaluatorSupportPage,
  EvaluatorSchedulePage,
  MentorDashboardPage,
  MentorGroupsPage,
  MentorTopicsPage,
  MentorSchedulePage,
  MentorFeedbackPage,
  MentorTopicDetailPage,
  MentorSupportPage,
  StudentDashboardPage,
  StudentSchedulePage,
  StudentTopicsPage,
  StudentMyTopicPage,
  StudentSupportPage,
  StudentGroupPage,
  TopicPoolsPage,
  TopicPoolDetailPage,
  MaintenancePage,
  DepartmentHeadDashboardPage,
  AssignEvaluatorsPage,
  ActivityLogsPage,
} from "@/pages";

// Helper function to adjust color brightness
const adjustColor = (color: string, amount: number) => {
  const hex = color.replace("#", "");
  const r = Math.max(0, Math.min(255, parseInt(hex.slice(0, 2), 16) + amount));
  const g = Math.max(0, Math.min(255, parseInt(hex.slice(2, 4), 16) + amount));
  const b = Math.max(0, Math.min(255, parseInt(hex.slice(4, 6), 16) + amount));
  return `#${r.toString(16).padStart(2, "0")}${g.toString(16).padStart(2, "0")}${b.toString(16).padStart(2, "0")}`;
};

const roleHomeMap: Record<string, string> = {
  admin: "/admin",
  mentor: "/mentor",
  evaluator: "/evaluator",
  student: "/student",
  departmenthead: "/department-head",
};

/** Redirects authenticated users to their role-based home page, or to /login if not logged in. */
function RoleBasedRedirect() {
  const { user, activeRole, isAuthenticated, isLoading } = useAuth();

  if (isLoading) return null;

  if (!isAuthenticated || !user) {
    return <Navigate to="/login" replace />;
  }

  return <Navigate to={roleHomeMap[activeRole || user.role] ?? "/login"} replace />;
}

function App() {
  // Apply saved theme color on app initialization
  useEffect(() => {
    const savedColor = localStorage.getItem("themeColor") || "#2c6090";
    document.documentElement.style.setProperty("--color-primary", savedColor);
    document.documentElement.style.setProperty("--color-primary-dark", adjustColor(savedColor, -20));
    document.documentElement.style.setProperty("--color-primary-light", adjustColor(savedColor, 20));
  }, []);
  return (
    <MaintenanceProvider>
      <AuthProvider>
        <SystemErrorProvider>
          <AnimatePresence mode="wait">
            <Routes>
              {/* Public Routes */}
              <Route path="/login" element={<LoginPage />} />
              <Route path="/maintenance" element={<MaintenancePage />} />
              <Route path="/403" element={<AccessDeniedPage />} />

              {/* Protected Admin Routes */}
              <Route
                path="/admin"
                element={
                  <ProtectedRoute allowedRoles={["admin"]}>
                    <AdminLayout />
                  </ProtectedRoute>
                }
              >
                <Route index element={<DashboardPage />} />
                <Route path="settings" element={<SettingsPage />} />
                <Route path="semesters" element={<SemestersPage />} />
                <Route path="users" element={<UsersPage />} />
                <Route path="projects" element={<ProjectsPage />} />
                <Route path="activity-logs" element={<ActivityLogsPage />} />
                <Route path="support" element={<SupportPage />} />
              </Route>

              {/* Protected Evaluator Routes */}
              <Route
                path="/evaluator"
                element={
                  <ProtectedRoute allowedRoles={["evaluator", "mentor"]}>
                    <EvaluatorLayout />
                  </ProtectedRoute>
                }
              >
                <Route index element={<EvaluatorDashboardPage />} />
                <Route path="projects" element={<EvaluatorProjectsPage />} />
                <Route path="schedule" element={<EvaluatorSchedulePage />} />
                <Route path="history" element={<EvaluatorHistoryPage />} />
                <Route path="review/:id" element={<EvaluatorReviewPage />} />
                <Route path="review" element={<EvaluatorReviewPage />} />
                <Route path="similarity" element={<EvaluatorSimilarityPage />} />
                <Route path="support" element={<EvaluatorSupportPage />} />
              </Route>

              {/* Protected Mentor Routes */}
              <Route
                path="/mentor"
                element={
                  <ProtectedRoute allowedRoles={["mentor", "evaluator"]}>
                    <MentorLayout />
                  </ProtectedRoute>
                }
              >
                <Route index element={<MentorDashboardPage />} />
                <Route path="groups" element={<MentorGroupsPage />} />
                <Route path="groups/:id" element={<MentorTopicDetailPage />} />
                <Route path="topics" element={<MentorTopicsPage />} />
                <Route path="topics/:id" element={<MentorFeedbackPage />} />
                <Route path="schedule" element={<MentorSchedulePage />} />
                <Route path="support" element={<MentorSupportPage />} />
                <Route path="topic-pools" element={<TopicPoolsPage />} />
                <Route path="topic-pools/:id" element={<TopicPoolDetailPage />} />
              </Route>

              {/* Protected Student Routes */}
              <Route
                path="/student"
                element={
                  <ProtectedRoute allowedRoles={["student"]}>
                    <StudentLayout />
                  </ProtectedRoute>
                }
              >
                <Route index element={<StudentDashboardPage />} />
                <Route path="my-topic" element={<StudentMyTopicPage />} />
                <Route path="topics" element={<StudentTopicsPage />} />
                <Route path="groups" element={<StudentGroupPage />} />
                <Route path="schedule" element={<StudentSchedulePage />} />
                <Route path="support" element={<StudentSupportPage />} />
              </Route>

              {/* Smart redirect: root goes to role-based home */}
              <Route path="/" element={<RoleBasedRedirect />} />
              {/* Protected DepartmentHead Routes */}
              <Route
                path="/department-head"
                element={
                  <ProtectedRoute>
                    <DepartmentHeadLayout />
                  </ProtectedRoute>
                }
              >
                <Route index element={<DepartmentHeadDashboardPage />} />
                <Route path="assign" element={<AssignEvaluatorsPage />} />
              </Route>

              {/* Redirect root to admin */}
              <Route path="/" element={<Navigate to="/admin" replace />} />

              {/* 404 — any unmatched route */}
              <Route path="*" element={<NotFoundPage />} />
            </Routes>
          </AnimatePresence>
        </SystemErrorProvider>
      </AuthProvider>
    </MaintenanceProvider>
  );
}

export default App;
