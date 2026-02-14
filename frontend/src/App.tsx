import { useEffect, useMemo, useState } from "react";
import Overview from "./pages/Overview";
import Controllers from "./pages/Controllers";
import ControllerDetail from "./pages/ControllerDetail";
import ControlRoomToggle from "./components/ControlRoomToggle";

type Route = "overview" | "controllers" | "controller";

type RouteState = {
  route: Route;
  controllerId?: string;
};

function parseHash(): RouteState {
  const hash = window.location.hash.replace("#", "");
  if (hash.startsWith("controller/")) {
    return { route: "controller", controllerId: hash.split("/")[1] };
  }
  if (hash === "controllers") {
    return { route: "controllers" };
  }
  return { route: "overview" };
}

export default function App() {
  const [route, setRoute] = useState<RouteState>(parseHash());
  const [controlRoomMode, setControlRoomMode] = useState(false);

  useEffect(() => {
    const handler = () => setRoute(parseHash());
    window.addEventListener("hashchange", handler);
    return () => window.removeEventListener("hashchange", handler);
  }, []);

  const content = useMemo(() => {
    if (route.route === "controllers") {
      return <Controllers />;
    }
    if (route.route === "controller" && route.controllerId) {
      return <ControllerDetail controllerId={route.controllerId} />;
    }
    return <Overview controlRoomMode={controlRoomMode} />;
  }, [controlRoomMode, route]);

  return (
    <div className={controlRoomMode ? "app control-room" : "app"}>
      <header className="app-header">
        <div>
          <h1>SiPass Health Dashboard</h1>
          <p className="subtle">Operations overview for controllers and access points</p>
        </div>
        <nav>
          <a href="#overview">Overview</a>
          <a href="#controllers">Controllers</a>
        </nav>
        <ControlRoomToggle enabled={controlRoomMode} onToggle={setControlRoomMode} />
      </header>
      <main className="app-main">{content}</main>
    </div>
  );
}
