import { QueryClientProvider } from '@tanstack/react-query';
import { useEffect } from 'react';
import { queryClient } from './app/queryClient';
import { AppRouter } from './app/router';
import { ToastStack } from './shared/ui/Toast';
import { useAuthStore } from './shared/auth/authStore';

export function App() {
  const hydrate = useAuthStore((s) => s.hydrate);
  useEffect(() => { hydrate(); }, [hydrate]);

  return (
    <QueryClientProvider client={queryClient}>
      <AppRouter />
      <ToastStack />
    </QueryClientProvider>
  );
}
