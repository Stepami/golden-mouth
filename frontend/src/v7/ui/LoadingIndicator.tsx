/** One optical size and stroke for waiting in buttons, rows and screens. */
export function LoadingIndicator() {
  return <svg className="loading-indicator" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" aria-hidden="true">
    {Array.from({length:8}, (_,i) => <path key={i} d="M12 3v3" transform={`rotate(${i*45} 12 12)`} opacity={(i+1)/8}/>)}
  </svg>;
}
