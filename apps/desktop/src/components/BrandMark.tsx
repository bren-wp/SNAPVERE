import React from "react";

/** Same vector silhouette as the product website's bundled favicon. */
export default function BrandMark({ className = "" }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 128 128" aria-hidden="true" focusable="false" fill="none">
      <rect width="128" height="128" rx="28" fill="#081b30" />
      <g stroke="#55e0e5" strokeWidth="7" strokeLinecap="round" strokeLinejoin="round">
        <path d="M44 27H27V44 M84 27H101V44 M27 84V101H44 M101 84V101H84" />
      </g>
      <path d="M79 36C51 20 38 49 61 62C84 75 72 108 43 93" stroke="#fff" strokeWidth="9" strokeLinecap="round"/>
      <circle cx="96" cy="94" r="5" fill="#55e0e5"/>
    </svg>
  );
}
