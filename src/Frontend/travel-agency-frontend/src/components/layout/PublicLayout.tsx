import Header from './Header';
import Footer from './Footer';
import RoutedOutlet from './RoutedOutlet';

export default function PublicLayout() {
  return (
    <div className="flex min-h-screen flex-col">
      <Header />
      <main className="flex-1 pt-16 lg:pt-20">
        <RoutedOutlet />
      </main>
      <Footer />
    </div>
  );
}
