export default function Navbar({ title }) {
  return (
    <header className="bg-white shadow p-4 flex justify-between items-center">
      <h1 className="text-xl font-semibold">{title}</h1>
      <div>
        <button className="bg-blue-500 text-white px-4 py-2 rounded-lg hover:bg-blue-600">
          Çıkış
        </button>
      </div>
    </header>
  );
}
