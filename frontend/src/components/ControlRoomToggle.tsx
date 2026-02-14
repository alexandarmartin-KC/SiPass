type Props = {
  enabled: boolean;
  onToggle: (value: boolean) => void;
};

export default function ControlRoomToggle({ enabled, onToggle }: Props) {
  return (
    <label className="control-room-toggle">
      <span>Control Room Mode</span>
      <input
        type="checkbox"
        checked={enabled}
        onChange={(event) => onToggle(event.target.checked)}
        aria-label="Toggle control room mode"
      />
      <span className="control-room-slider" aria-hidden="true" />
    </label>
  );
}
