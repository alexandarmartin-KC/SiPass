type Props = {
  title: string;
  message: string;
};

export default function Banner({ title, message }: Props) {
  return (
    <div className="panel" style={{ borderColor: "#d8a36b", background: "#fff5e6" }}>
      <strong>{title}</strong>
      <p className="subtle" style={{ marginTop: 6 }}>
        {message}
      </p>
    </div>
  );
}
