/** Shared vector silhouette with the official site/favicon.svg. */
export default function BrandMark({ className = "" }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 128 128" aria-hidden="true" focusable="false" fill="none">
      <rect x="3" y="3" width="122" height="122" rx="27" fill="#151831" stroke="#8050ed" strokeWidth="4" />
      <path d="M23 104C36 66 59 33 103 20C95 65 74 87 34 105C49 82 70 59 92 33C67 50 47 73 23 104Z" fill="#9e83f6" />
      <path d="M24 105C49 74 71 52 102 20" stroke="#f7f5ff" strokeWidth="4" strokeLinecap="round" />
      <path d="M44 88C53 68 72 48 90 36" stroke="#d7c9ff" strokeWidth="2" opacity=".75" />
    </svg>
  );
}
