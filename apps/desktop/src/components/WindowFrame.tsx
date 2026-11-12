import React from "react";

// Obični prozor s native OS naslovnom trakom (decorations:true). Bez custom chroma.
export default function WindowFrame({
  children,
}: {
  title?: string;
  children: React.ReactNode;
}) {
  return <div className="flex h-full w-full flex-col bg-bg text-text">{children}</div>;
}
