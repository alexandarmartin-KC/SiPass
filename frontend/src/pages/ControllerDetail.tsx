type Props = {
  controllerId: string;
};

export default function ControllerDetail({ controllerId }: Props) {
  return (
    <section className="panel">
      <h2>Controller {controllerId}</h2>
      <p className="subtle">Detail view pending baseline integration.</p>
    </section>
  );
}
