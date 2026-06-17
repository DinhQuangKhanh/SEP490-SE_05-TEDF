import { useNavigate } from "react-router-dom";
import { RegisterTopicModal } from "@/components/lecturer";

/**
 * Dedicated page for registering/proposing a new topic.
 * Reuses the multi-step RegisterTopicModal form; on close (or success) the
 * lecturer is returned to the research topic repository (`/lecturer`).
 */
export function TopicCreatePage() {
  const navigate = useNavigate();

  return (
    <div className="flex-1 overflow-y-auto bg-slate-100">
      <RegisterTopicModal isOpen onClose={() => navigate("/lecturer")} />
    </div>
  );
}
