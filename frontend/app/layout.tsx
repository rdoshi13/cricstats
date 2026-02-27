import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "CricStats Dashboard",
  description: "Upcoming matches and series from CricStats API"
};

export default function RootLayout({
  children
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
